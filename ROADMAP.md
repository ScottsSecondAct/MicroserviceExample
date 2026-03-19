
# Roadmap

## Explicitly Deferred

The following are real enterprise CRM features that have been deliberately excluded from the roadmap. They do not add credibility proportional to their implementation cost at this stage, or they depend on business decisions (pricing model, target market, distribution channel) that have not yet been made.

| Feature | Why deferred |
|---------|-------------|
| **Marketing automation** | Campaigns, lead nurturing, email sequences, and drip workflows are a separate product surface. They require a dedicated campaign engine, list segmentation, unsubscribe compliance (CAN-SPAM/GDPR), and send-time optimization. Better addressed by integrating a dedicated tool (Mailchimp, Brevo) via webhook/API than building in-house. |
| **Customer service / ticketing** | Helpdesk and support ticket management (case routing, SLA tracking, agent queues) is a distinct product line. Depth required to be credible here is comparable to building a second CRM. Integrate with Zendesk or Intercom instead. |
| **Territory management** | Assigning and enforcing geographic or account-based sales territories requires a rules engine, territory hierarchy, and conflict-resolution logic. High complexity, low frequency of need outside large enterprise sales orgs. |
| **Advanced forecasting** | Statistical pipeline forecasting, quota management, rep attainment tracking, and commit/best-case/pipeline categories require substantial data history and a forecasting model. The reporting foundation (v3.5) is a prerequisite; this builds on top of it. |
| **CPQ (Configure-Price-Quote)** | Product catalog, pricing rules, discount approvals, and quote document generation are a full application in their own right. Requires a separate ProductService and document templating engine. |
| **Mobile app** | Native iOS/Android apps require a separate build pipeline, app store distribution, push notification infrastructure, and offline sync. Responsive web (already in scope) is sufficient for MVP. |
| **Marketplace / app ecosystem** | Third-party integrations via an app store, OAuth app registration, and a published extension API require developer relations, documentation infrastructure, and ongoing partner support. Post-GA consideration. |
| **Multi-currency** | Storing, converting, and displaying deal values in multiple currencies requires exchange-rate feeds, a currency normalization layer in reporting, and locale-aware formatting throughout the UI. Required only when targeting global sales orgs. |
| **Multi-language (i18n)** | Full UI localization requires extracting all strings into resource files, RTL layout support, and ongoing translation maintenance. Addressable via community contribution post-launch. |
| **Autonomous AI agents** | Agents that take CRM actions on behalf of users (auto-log calls, auto-update deal stages, auto-draft emails) require a higher trust model, approval workflows, and explainability features before enterprise buyers will accept them. The AI foundation (v4.3) is a prerequisite. |

---

## v1.0 — Current State ✅

Working two-service authentication system with a React frontend.

- [x] AuthService — registration, login, JWT issuance (2-hour expiry)
- [x] UserManagementService — user profiles with role assignment
- [x] Synchronous HTTP inter-service communication via `HttpClientFactory`
- [x] PostgreSQL + Entity Framework Core (database-per-service)
- [x] SharedLibrary — shared DTOs and `UserRole` enum
- [x] JWT Bearer authentication with claims (UserId, Email, Role)
- [x] Password hashing and verification
- [x] Layered architecture: Controller → IService → IRepository → DbContext
- [x] xUnit + Moq + FluentAssertions test suite
- [x] React frontend — login, register, and profile pages
- [x] Vite dev proxy (single entry point for local development)
- [x] Swagger/OpenAPI on both services

---

## v1.1 — Infrastructure Foundation

Prerequisite for all CRM work. No new features — only the infrastructure that makes adding services safe and reliable.

- [x] **Fix role duplication** — remove `Role` from `AuthService.User`; on login, fetch current role from UserManagementService synchronously before minting the JWT
- [x] **Async registration** — convert `RegistrationService` from a synchronous HTTP call to publishing a `UserRegistered` event; UserManagementService becomes a consumer instead of being called directly
- [x] **RabbitMQ + MassTransit** — add to Docker Compose; establish event publishing/consuming conventions (CorrelationId, OccurredAt, EventType base fields)
- [x] **YARP API Gateway** — single entry point for all services; centralize JWT validation, routing, and CORS; update Vite proxy to target gateway only
- [x] **Docker Compose** — containerize both services and PostgreSQL; service-name-based DNS replaces hardcoded `ServiceUrls` config
- [x] **Health checks** — `AddHealthChecks()` with DB and RabbitMQ probes on all services; gateway aggregates downstream health
- [x] **OpenTelemetry** — distributed tracing across services; W3C `traceparent` header propagation through HTTP calls and message headers
- [x] **Split SharedLibrary** — break into topic packages (`SharedLibrary.Auth`, `SharedLibrary.Messaging`) so a change to one domain's events doesn't force a rebuild of unrelated services

---

## v1.2 — Contacts & Accounts ✅

Core CRM entities. The building blocks every other CRM feature depends on.

- [x] **ContactService** — full CRUD; status lifecycle (Lead → Prospect → Customer → Churned); owner assignment; validates AccountId against AccountService on create/update; publishes `ContactCreated`, `ContactStatusChanged`, `ContactDeleted`; filterable list (`?status`, `?ownerId`, `?accountId`)
- [x] **AccountService** — full CRUD; firmographic fields (industry, size, website, address); publishes `AccountCreated`, `AccountDeleted`; enums serialized as strings
- [x] **SharedLibrary.Contacts / SharedLibrary.Accounts** — topic-scoped event packages for new services
- [x] **UserManagementService: team endpoint** — `GET /api/users/team` returning lightweight projections (UserId, DisplayName, Role) for owner assignment dropdowns
- [x] **Gateway routes** — `/contacts/**` and `/accounts/**` with JWT authorization policy; PathRemovePrefix transforms
- [x] **Frontend: React Router** — React Router v6 with BrowserRouter, nested routes, ProtectedRoute, and Layout with NavLink active state
- [x] **Frontend: React Query** — TanStack Query v5 (`useQuery`, `useMutation`, cache invalidation on mutations)
- [x] **Frontend: per-domain API client modules** — `apiClient.js` base (JWT header injection) + `auth.api.js`, `users.api.js`, `contacts.api.js`, `accounts.api.js`
- [x] **Frontend: Contact module** — list with status/owner filter dropdowns, detail with status lifecycle transition buttons, create/edit form with account and owner selects
- [x] **Frontend: Account module** — list, detail with embedded contacts table, create/edit form with address section

---

## v1.3 — Deals & Pipeline ✅

The sales pipeline — the primary daily-use feature for sales reps.

- [x] **DealService** — pipeline stages (seeded: Prospecting, Proposal, Negotiation, Closed Won, Closed Lost); deal CRUD; deal-contact associations with role (Decision Maker, Influencer, Champion); validates ContactId and AccountId on create; publishes `DealCreated`, `DealStageChanged`, `DealClosed`
- [x] **SharedLibrary.Deals** — event package
- [x] **Gateway routes** — `/api/deals/**` and `/api/pipeline/**`
- [x] **DealService subscribes to `ContactDeleted`** — handle deals whose associated contact is removed
- [x] **Frontend: Pipeline board** — Kanban view grouped by stage; drag-and-drop stage updates
- [x] **Frontend: Deal detail** — associated contacts, account, value, probability, expected close date, activity timeline stub
- [x] **Frontend: Deal create/edit form** — stage selector, contact/account association

---

## v1.4 — Activities ✅

The activity log ties contacts, deals, and reps together into a complete interaction history.

- [x] **ActivityService** — activity types: Call, Email, Meeting, Task, Note; references ContactId, DealId, AccountId (all optional); scheduled/completed timestamps for tasks; publishes `ActivityLogged`, `TaskCompleted`
- [x] **SharedLibrary.Activities** — event package
- [x] **Gateway routes** — `/api/activities/**`
- [x] **Frontend: Activity log form** — quick-add accessible from Contact, Deal, and Account detail pages
- [x] **Frontend: Activity timeline** — chronological feed on Contact and Deal detail pages
- [x] **Frontend: Task list** — all incomplete tasks assigned to the current user

---

## v1.5 — Reporting & Dashboards ✅

Visibility into pipeline health and rep activity, powered by an event-driven read model.

- [x] **ReportingService** — subscribes to `DealCreated`, `DealStageChanged`, `DealClosed`, `ActivityLogged`, `ContactStatusChanged`; maintains denormalized projections (pipeline value by stage, activity counts by rep, contact funnel by status); no external write API
- [x] **Gateway routes** — `/reports/**` with JWT authorization policy
- [x] **Frontend: Dashboard** — pipeline summary chart (value by stage), activity counts per rep, contact funnel by status

Dashboard data will lag source services by seconds due to the event-driven projection model. This is acceptable for all reporting use cases.

---

## v1.6 — Enterprise UI Redesign ✅

Upgrade the frontend from a functional but basic layout to a professional, enterprise-grade experience on par with tools like Salesforce, HubSpot, or Linear.

### Shell & Navigation
- [x] **Left sidebar layout** — replace horizontal top nav with a fixed left sidebar; group nav items by domain (CRM: Contacts, Accounts, Pipeline; Productivity: Tasks; Insights: Dashboard); icon + label per item; collapsible to icon-only mode for more screen real estate
- [x] **Top bar** — global search input, notification bell, user avatar dropdown (profile link + logout); replace plain email display with avatar initials or photo
- [x] **Breadcrumbs** — contextual breadcrumbs on detail and form pages (e.g. Contacts › Acme Corp › Edit)

### Component Library
- [x] **Adopt Tailwind CSS + shadcn/ui** — replace hand-written CSS with Tailwind utility classes; use shadcn/ui for production-quality primitives (Dialog, Sheet/Slideover, DropdownMenu, Toast, Skeleton, Select, Combobox)
- [x] **Slideover panels** — open create/edit forms in a right-hand Sheet instead of navigating to a full page; reduces context loss for power users

### Data Tables
- [x] **Sortable columns** — click column headers to sort; indicator shows active sort direction
- [x] **Pagination** — page size selector + prev/next controls; row count displayed
- [x] **Inline row actions** — hover reveals Edit and Delete icon buttons in a rightmost column; delete triggers a confirmation Dialog
- [x] **Bulk select** — checkbox column; bulk-delete or bulk-status-change action bar appears when rows are selected

### Feedback & States
- [x] **Toast notifications** — replace inline form-level success/error messages with a toast stack (shadcn/ui Toaster); non-blocking, auto-dismisses
- [x] **Loading skeletons** — replace plain "Loading…" text with content-shaped skeleton loaders on tables and detail cards
- [x] **Guided empty states** — replace plain empty text with an illustration + heading + CTA button (e.g. "No contacts yet — Add your first contact")
- [x] **Confirmation dialogs** — all destructive actions (delete contact, remove deal contact, etc.) require explicit confirmation via Dialog before firing

### Dashboard
- [x] **KPI stat cards** — large metric + label + trend indicator (up/down vs prior period) at the top of the Dashboard page
- [x] **Interactive charts** — hover tooltips and clickable segments on pipeline and funnel charts (Recharts already installed)

### Enterprise Readiness (UI layer)
- [x] **Admin section** — user list page visible only to Admin role; promote/demote role, deactivate account actions
- [x] **Responsive layout** — sidebar collapses to a hamburger drawer on narrow viewports

---

## v2.0 — Hardening & Production Readiness

- [x] **Refresh tokens** — implement refresh token rotation in AuthService; issue short-lived JWTs alongside opaque refresh tokens stored in the DB; `POST /api/auth/refresh` rotates the token and issues a new JWT
- [x] **Secrets management (Phase 1)** — move JWT key, DB passwords, and RabbitMQ credentials out of `appsettings.json` and `docker-compose.yml` into environment variables; `.env.example` documents all required variables; Phase 2 (Vault / cloud secret store) remains open
- [x] **Structured logging** — consistent log fields (correlationId via OTel trace ID, userId, serviceId) across all services; JSON-formatted console output; ships to a central log aggregator
- [x] **Dead-letter queue handling** — monitoring and alerting on DLQ depth for all RabbitMQ queues; MassTransit retry policies with exponential backoff on all consumers
- [x] **Rate limiting** — at the gateway; per-IP and per-user limits
- [x] **Soft delete + audit trail** — `IsDeleted`/`DeletedAt` on all CRM entities; lightweight audit log (who changed what and when) per service
- [x] **Integration test suite** — end-to-end tests covering registration, contact creation, and deal creation across live services in a Docker Compose test environment
- [x] **CRM-specific roles** — expand `UserRole` beyond `Member`/`Admin` to include `SalesRep` and `Manager`; migrate enum out of SharedLibrary into UserManagementService's own domain

---

## v2.1 — Enterprise User Management ✅

Replaces the open self-registration model with an admin-controlled identity system suitable for enterprise deployment. Option A (admin-managed accounts) was selected and fully implemented. Option B (SSO/OIDC) is deferred to a future version.

### Account Provisioning — Option A (admin-managed)
- [x] **Disable public self-registration** — `POST /api/registration/register` gated behind Admin-only authorization policy
- [x] **Invite flow** — Admin calls `POST /api/users/invite`; AuthService generates a crypto-secure 48h token and sends an invite email via MailKit/SMTP; recipient sets password via `POST /api/registration/accept-invite`; token is single-use
- [x] **`Unassigned` holding state** — newly invited users start as `Unassigned`; Admin explicitly promotes them to `Member`, `SalesRep`, or `Manager` before they can access CRM data

### Role & Account Administration
- [x] **Admin: user list** — `GET /api/users` (Admin only) returning all users with email, display name, role, and active status; Admin section in frontend
- [x] **Admin: role assignment** — `PATCH /api/users/{id}/role` (Admin only)
- [x] **Admin: deactivate / reactivate** — `PATCH /api/users/{id}/status`; deactivated users rejected at login and refresh; soft-delete, not hard-delete
- [x] **Admin: resend invite** — re-issue a fresh invite token for a pending user

### Password Management
- [x] **Forgot password flow** — `POST /api/auth/forgot-password` generates a signed reset token; `POST /api/auth/reset-password` consumes it; tokens are single-use with 1-hour expiry
- [x] **Force password change on first login** — invite-accepted users flagged `MustChangePassword`; frontend detects the claim and redirects to change-password before allowing further navigation
- [x] **Password policy** — minimum length, complexity rules enforced at AuthService; policy configurable via `appsettings.json`

### Audit Trail (identity events)
- [x] **Identity audit log** — records admin actions (invite sent, role changed, account deactivated) with timestamp and actor UserId; stored in UserManagementService; accessible via `GET /api/users/audit` (Admin only)

### Frontend
- [x] **Auth UI** — invite accept flow, forgot/reset password pages, forced change-password page, password policy enforcement with strength indicator

---

## v2.2 — Username Login & Tenancy Foundation

Adds username-based login and lays the structural groundwork for multi-tenancy so the username feature is built on the correct schema from day one — avoiding a breaking migration when v3.0 multi-tenancy is implemented.

Supports all three planned deployment models:
- **On-prem / dedicated cloud** — single tenant; `TenantId` is always the same value, invisible to users; username `admin` works without conflict
- **Shared cloud (SaaS)** — multiple tenants on one instance; tenant resolved from subdomain at the gateway; `(TenantId, Username)` composite uniqueness allows `admin` in every tenant

### Tenant Entity (new)
- [ ] **Tenant table** — add a `Tenant` entity to `AuthDbContext` and `UserManagementDbContext`; fields: `TenantId` (PK), `Slug` (unique), `DisplayName`, `CreatedAt`
- [ ] **Default tenant seed** — single-tenant deployments seed one tenant on startup via `appsettings.json` `DefaultTenant` section; invisible to users

### TenantId on Users and Profiles
- [ ] **`TenantId` FK on `AuthService.User`** — single-tenant deployments always use the seeded default; no UI exposure needed
- [ ] **`TenantId` FK on `UserManagementService.UserProfile`** — same pattern

### Username Login
- [ ] **`Username` field on `User`** — nullable string with composite `(TenantId, Username)` unique constraint; replaces any global unique index
- [ ] **Admin seed** — default admin gets `Username = "admin"` (or value from `DefaultAdmin` config)
- [ ] **Registration** — username auto-derived from email prefix (e.g. `john.doe@corp.com` → `john.doe`); numeric suffix appended on collision within tenant
- [ ] **`LoginRequest.EmailOrUsername`** — rename `Email` field; drop `[EmailAddress]` validation; `LoginService` branches on `@` to look up by email or by `(TenantId, Username)`
- [ ] **Forgot-password stays email-only** — email is still required to send a reset link

### Admin Provisioning Flow
- [ ] **Startup seed** preserved for single-tenant deployments (on-prem / dedicated cloud) where `DefaultTenant` + `DefaultAdmin` config is present
- [ ] **Provisioning endpoint** — `POST /api/tenants/provision` (bootstrap-secret auth) for shared cloud tenant creation; creates tenant + first admin atomically

### Gateway Update
- [ ] **Subdomain → `X-Tenant-Id`** — YARP middleware extracts subdomain from `Host` header and forwards `X-Tenant-Id` to downstream services for shared cloud deployments; single-tenant deployments fall back to the default tenant

### Frontend
- [ ] **Login form** — label changes to "Email or username"; `type="text"`, `autoComplete="username"`

---

## v3.0 — Multi-Tenancy

Allows multiple independent organizations to share the same deployment with full data isolation. This is a significant cross-cutting refactor that touches every service.

### Prerequisites (complete before any multi-tenancy work)
- [ ] **Introduce EF Core migrations** — `EnsureCreated()` cannot add columns to existing databases; all 6 services must be migrated to `dotnet ef migrations` before any schema changes can be applied reliably across environments

### Tenant Resolution
- [ ] **Tenant identification strategy** — choose and implement one: subdomain-based (`acme.yourapp.com`), header-based (`X-Tenant-Id`), or path-based (`/t/{tenantId}/...`); subdomain is the most enterprise-standard approach
- [ ] **Tenant registry** — a new lightweight TenantService (or a table in an existing service) that maps tenant identifiers to tenant IDs and holds per-tenant configuration
- [ ] **Gateway tenant extraction** — YARP middleware resolves the incoming request to a `TenantId` and forwards it as a trusted internal header to all downstream services; rejects requests with an unresolvable tenant

### Data Isolation (Row-Level)
Row-level isolation with a shared database is the most practical starting point — strongest isolation (DB-per-tenant) can be layered on later for high-value customers.

- [ ] **Add `TenantId` to all entities** — every CRM entity across all 6 services gains a non-nullable `TenantId` column; covered by migrations
- [ ] **`ITenantContext` service** — scoped DI service populated by middleware from the incoming tenant header; available throughout the request pipeline
- [ ] **EF global query filters** — each `DbContext` registers `.HasQueryFilter(e => e.TenantId == _tenantContext.TenantId)` on every entity; ensures all queries are automatically scoped without per-query changes
- [ ] **Write-path tenant stamping** — repositories set `TenantId` from `ITenantContext` on every created entity; enforced at the repository base class level

### Auth & Identity
- [ ] **`TenantId` in JWT claims** — AuthService encodes the resolved tenant into the JWT at login time; downstream services extract it from the token as a secondary verification
- [ ] **Tenant-scoped registration** — users register within a specific tenant context; cross-tenant access is not permitted
- [ ] **Admin account provisioning** — disable public self-registration; each tenant gets a super-admin account created by the platform operator; super-admin invites additional users within their tenant

### Messaging
- [ ] **`TenantId` on all events** — add `TenantId` to `BaseEvent` in `SharedLibrary.Messaging`; all publishers set it; all consumers scope their DB operations using it
- [ ] **Consumer tenant context** — MassTransit consumers populate `ITenantContext` from the incoming message before calling any service or repository method

### Service-to-Service HTTP
- [ ] **Forward tenant header** — all inter-service HTTP clients (`AccountClient`, `ContactClient`, etc.) forward the `X-Tenant-Id` header on every outbound request; validated by the receiving service

### Testing
- [ ] **Multi-tenant integration tests** — integration tests create two tenants and assert that data created under tenant A is not visible to tenant B; covers both the HTTP API and event-consumer paths

---

## v3.1 — Full-Text Search & Saved Views

Fast search and persistent filter views — the features sales reps use most on a daily basis.

### Search
- [ ] **PostgreSQL full-text search** — add `tsvector` generated columns and GIN indexes to Contacts, Accounts, and Deals; update repositories with `ts_rank`-ordered search queries
- [ ] **Unified search endpoint** — `GET /api/search?q=…` at the gateway fans out to ContactService, AccountService, and DealService in parallel and merges ranked results; returns entity type, id, display name, and a context snippet
- [ ] **Per-service search endpoints** — `GET /api/contacts?q=`, `GET /api/accounts?q=`, `GET /api/deals?q=` accepting a free-text query parameter alongside existing filters

### Saved Views
- [ ] **SavedView entity** — new table (per-user, per-entity-type): stores a name, filter JSON blob, column ordering, and sort direction; owned by UserManagementService or a new ViewService
- [ ] **CRUD endpoints** — `GET/POST/PUT/DELETE /api/views?entity=contacts` (JWT-scoped to current user)

### Frontend
- [ ] **Global search overlay** — keyboard shortcut (`Cmd/Ctrl+K`) opens a modal; results grouped by type with icon and snippet; click navigates to record detail
- [ ] **Saved views sidebar** — each list page (Contacts, Accounts, Deals) shows a collapsible panel of saved views for the current user; clicking a view applies its filters and sort; save/rename/delete inline

---

## v3.2 — Import & Export

Unblocks enterprise migration and bulk data operations — a hard requirement for any procurement conversation.

### Export
- [ ] **CSV export endpoint** — `GET /api/contacts/export`, `/api/accounts/export`, `/api/deals/export`; respects all current query filters; streams the file directly; column headers match display labels
- [ ] **Activity export** — include `GET /api/activities/export` filtered by contactId, dealId, or accountId

### Import
- [ ] **CSV import endpoint** — `POST /api/contacts/import`, `/api/accounts/import`; multipart form upload; server validates each row against model constraints; returns a structured result: rows imported, rows skipped, per-row errors with line numbers
- [ ] **Field mapping** — import endpoint accepts a `mapping` parameter (JSON object of CSV column → model field) so files with non-standard headers can be mapped without pre-processing

### Frontend
- [ ] **Export button** — toolbar button on every list view; respects active filters; triggers file download
- [ ] **Import wizard** — three-step Sheet (upload CSV → map columns → review and confirm); shows row-level error summary after import; success toast with imported count

---

## v3.3 — Multiple Pipelines & Configurable Stages

Sales teams running different products or motions need separate pipelines with their own stage names, probabilities, and required fields.

### Pipeline Service Changes (DealService)
- [ ] **Pipeline entity** — `Pipeline` table: `PipelineId`, `Name`, `IsDefault`, `TenantId`; seed one "Default Sales Pipeline" from existing hardcoded stages on migration
- [ ] **Stage entity** — `Stage` table: `StageId`, `PipelineId` (FK), `Name`, `Order`, `ProbabilityHint`, `Color`; replaces the `DealStage` enum; existing deals migrate to stages in the default pipeline
- [ ] **Deal.StageId FK** — replace `Stage` enum column with FK to `Stage` entity; update all queries, events, and projections accordingly
- [ ] **Pipeline CRUD endpoints** — `GET/POST/PUT/DELETE /api/pipelines`; `GET/POST/PUT/DELETE /api/pipelines/{id}/stages` (Admin only for write operations)
- [ ] **Pipeline board endpoint** — `GET /api/pipeline?pipelineId={id}` returns deals grouped by that pipeline's stages; default pipeline used when omitted

### Frontend
- [ ] **Pipeline selector** — dropdown above the Kanban board to switch between pipelines
- [ ] **Stage manager** — admin-only settings page to create/rename/reorder/delete pipelines and their stages; color picker per stage
- [ ] **Deal form: pipeline + stage** — deal create/edit form shows pipeline selector first, then stage dropdown filtered to that pipeline's stages

---

## v3.4 — Hierarchical Accounts & Relationship Graph

Parent/child account hierarchies are standard in B2B CRM — subsidiaries roll up to parent companies.

### AccountService Changes
- [ ] **ParentAccountId self-referential FK** — nullable `ParentAccountId` on `Account`; add a `GET /api/accounts/{id}/children` endpoint returning direct children
- [ ] **Hierarchy endpoint** — `GET /api/accounts/{id}/hierarchy` returns the full subtree (account + all descendants) up to a configurable depth limit (default 3)
- [ ] **Roll-up projections** — `GET /api/accounts/{id}/summary` returns: total open deal value (summed from DealService via sync HTTP), total contact count, total activity count (all inclusive of child accounts)

### Frontend
- [ ] **Parent account selector** — account create/edit form includes a combobox to assign a parent; shows breadcrumb trail if nested (e.g. `Salesforce → Salesforce EMEA`)
- [ ] **Child accounts panel** — account detail page includes a collapsible "Subsidiaries" section listing direct children with a link to each; "View full hierarchy" link opens a tree view
- [ ] **Hierarchy tree view** — dedicated page or modal showing the full org chart for a root account; click any node to navigate to that account's detail

---

## v3.5 — Configurable Dashboards & Report Builder

Move beyond fixed charts to let users build and save their own reports and arrange dashboards to match their workflow.

### ReportingService Changes
- [ ] **Report definition entity** — `SavedReport`: `ReportId`, `UserId`, `Name`, `EntityType`, `GroupByField`, `AggregateField`, `AggregateFunction` (Count/Sum/Avg), `Filters` (JSON), `DateRangeField`, `DateRangePreset`
- [ ] **Report execution endpoint** — `POST /api/reports/run` accepts a report definition (inline or by saved ID) and returns grouped aggregate results; no pre-computation — runs a live query against the read-model projections
- [ ] **CRUD for saved reports** — `GET/POST/PUT/DELETE /api/reports/saved`

### Dashboard Widgets
- [ ] **Widget registry** — small set of configurable widget types: `PipelineValueByStage`, `DealsClosedThisPeriod`, `ActivityCountByRep`, `ContactFunnelByStatus`, `TopOpenDeals`, `RecentActivity`
- [ ] **Dashboard layout entity** — per-user JSON blob storing widget type, position, and per-widget configuration (pipeline filter, date range, rep filter, etc.)
- [ ] **Dashboard CRUD** — `GET/PUT /api/dashboard` (JWT-scoped; reads and writes the current user's layout)

### Frontend
- [ ] **Report builder page** — choose entity type, group by field, aggregate, date range filter; live preview table; save button
- [ ] **Saved reports list** — table of saved reports with run/edit/delete actions; exported to CSV via the export endpoint
- [ ] **Configurable dashboard** — "Edit layout" mode; add widget from predefined list; drag to reorder; per-widget settings popover (date range, filter)

---

## v3.6 — Email Integration: BCC Capture

Connect daily email work to CRM records without requiring OAuth calendar access or a browser extension.

### Email Capture Flow
- [ ] **Per-contact BCC address** — each Contact gets a deterministic BCC address: `crm+{contactId}@{inbound-domain}`; displayed on contact detail
- [ ] **Inbound SMTP webhook** — configure SendGrid Inbound Parse, Mailgun Routes, or Postal to POST parsed email payloads to `POST /api/activities/email-inbound`; endpoint is gateway-public but validated by a shared secret header
- [ ] **Email-to-activity mapping** — ActivityService parses the inbound webhook: extracts subject, sender, timestamp, plain-text body; creates an `Email` activity linked to the contact resolved from the BCC address; publishes `ActivityLogged`
- [ ] **Duplicate guard** — deduplicate by `Message-ID` header; idempotent insert

### Frontend
- [ ] **BCC address chip** — prominent copy button on contact detail header showing the contact's unique BCC address with a "Copy for BCC" tooltip
- [ ] **Email activity card** — email entries in the activity timeline show sender, subject, and a collapsible body preview; distinguish visually from manually logged activities

---

## v3.7 — Workflow Automation

A "when X, do Y" rule engine that automates routine CRM work without code.

### WorkflowService (new service)
- [ ] **WorkflowRule entity** — `RuleId`, `TenantId`, `Name`, `IsActive`, `TriggerEntityType`, `TriggerEvent` (Created / Updated / FieldChanged / TimeSinceLastActivity), `TriggerField` (for FieldChanged), `Conditions` (JSON array), `Actions` (JSON array)
- [ ] **Condition model** — `{ field, operator (eq/neq/gt/lt/contains/isNull), value }`; supports AND across multiple conditions
- [ ] **Action types** — `UpdateField` (set a field value on the trigger record), `CreateActivity` (create a Task assigned to the record owner), `SendWebhook` (POST JSON payload to a URL), `AssignOwner` (set ownerId to a specific user or round-robin from a list)
- [ ] **CRUD endpoints** — `GET/POST/PUT/DELETE /api/workflows` (Admin/Manager only)
- [ ] **Event consumer** — subscribes to all domain events (`ContactCreated`, `DealStageChanged`, etc.); evaluates matching active rules; executes actions synchronously; logs execution result to an audit table
- [ ] **Time-based trigger** — a scheduled job (Quartz.NET or a cron consumer) scans for rules with `TimeSinceLastActivity` trigger and fires for matching records

### Frontend
- [ ] **Workflow list page** — admin settings section; table of rules with active toggle, last-triggered timestamp, run count
- [ ] **Rule builder** — step-by-step form: choose trigger (entity + event) → add conditions → add actions; each action type shows context-appropriate fields (field selector, user picker, URL input); save and activate

---

## v3.8 — Advanced RBAC: Record Visibility & Field-Level Security

Closes the gap between "admin vs. user" and the row-level and column-level access controls that enterprise security reviews require.

### Record Visibility Rules
- [ ] **Visibility policy model** — per-role configuration: `Own` (see only records you own), `Team` (see records owned by anyone in your team), `All` (see everything); configured per entity type (Contacts, Accounts, Deals)
- [ ] **Team entity** — `Team` table in UserManagementService: `TeamId`, `Name`, `ManagerUserId`; many-to-many `TeamMember`; endpoint `GET /api/teams` for assignment dropdowns
- [ ] **Visibility enforcement** — EF query filter per entity checks the requesting user's role → policy → applies `WHERE OwnerId = @userId`, `WHERE OwnerId IN (team members)`, or no filter; policy config fetched from UserManagementService at service startup and cached with a TTL
- [ ] **Owner assignment** — ensure every CRM entity (Contact, Account, Deal, Activity) has an `OwnerId` field; currently partial — audit and fill gaps

### Field-Level Security
- [ ] **FieldPermission table** — `{ Role, EntityType, FieldName, CanRead, CanWrite }`; admin-managed; stored in UserManagementService
- [ ] **Response filtering middleware** — after controller action, walk the response DTO and null-out fields the requesting user's role cannot read; enforced via a `[FieldSecured]` attribute + action filter
- [ ] **Write validation** — service layer checks field-write permissions before applying updates; returns 403 on violation with a clear message identifying the restricted field

### Frontend
- [ ] **Team management page** — admin creates teams, assigns members, sets manager; member picker with role filter
- [ ] **Visibility policy configurator** — per-role, per-entity-type dropdowns (`Own / Team / All`) in the admin settings section
- [ ] **Field permission matrix** — grid of roles × fields per entity with read/write checkboxes; changes saved atomically

---

## v4.0 — Custom Fields

Let admins extend the built-in entities with their own fields without code changes — the single most-requested feature in any CRM evaluation.

### Schema
- [ ] **CustomFieldDefinition entity** — `FieldId`, `TenantId`, `EntityType` (Contact / Account / Deal), `FieldName` (API key), `Label`, `FieldType` (Text / Number / Date / Boolean / Picklist / Lookup), `IsRequired`, `Options` (JSON array for Picklist), `SortOrder`, `IsActive`
- [ ] **`CustomFields` JSON column** — add a `jsonb` column to `contacts`, `accounts`, and `deals` tables; stores `{ fieldName: value }` per record; no EAV rows — one column per entity
- [ ] **CRUD for definitions** — `GET/POST/PUT/DELETE /api/custom-fields?entity=contacts` (Admin only); validates `FieldName` uniqueness per tenant + entity type
- [ ] **Validation on record save** — services load active field definitions for the entity type and validate `CustomFields` payload: required fields present, type coercion correct, picklist values in allowed set; returns structured field-level errors on failure
- [ ] **Search indexing** — extend full-text search (v3.1) to index string and text custom field values in the `tsvector` column via a trigger

### Frontend
- [ ] **Custom field manager** — admin settings page; per-entity tab; add/edit/reorder/deactivate fields; field type selector with type-specific options (min/max for Number, option list for Picklist)
- [ ] **Dynamic form rendering** — Contact/Account/Deal create and edit forms append a "Custom Fields" section rendered from the field definitions; each field type renders the appropriate input (text, number, date picker, toggle, select)
- [ ] **Custom fields in list views** — column picker on list pages allows adding any active custom field as a visible column; sort and filter supported for Text, Number, and Date types

---

## v4.1 — Custom Objects

Extends the custom fields foundation into fully user-defined entity types — the feature that separates a platform from an application.

### ObjectDefinition Service
- [ ] **ObjectDefinition entity** — `ObjectId`, `TenantId`, `ApiName` (URL-safe slug), `SingularLabel`, `PluralLabel`, `Icon`, `IsActive`; stored alongside `CustomFieldDefinition`
- [ ] **Metadata-driven router** — a generic `CustomObjectController` handles `GET/POST/PUT/DELETE /api/objects/{apiName}` and `GET/POST/PUT/DELETE /api/objects/{apiName}/{id}`; dispatches to a generic service that reads field definitions and validates/stores records in a single `custom_records` table with a `jsonb` `data` column
- [ ] **Relationship definitions** — `ObjectRelationship` entity allows relating a custom object to Contact, Account, Deal, or another custom object; stored as a foreign-key field in `CustomFieldDefinition` with `FieldType = Lookup`
- [ ] **CRUD endpoints for definitions** — `GET/POST/PUT/DELETE /api/object-definitions` (Admin only)

### Frontend
- [ ] **Object builder** — admin settings page; create/edit/delete object definitions with field manager (extends v4.0 custom field manager); preview of the generated list and form layout
- [ ] **Auto-generated list view** — each active custom object gets a nav entry and a list page with sortable columns (fields marked `ShowInList = true`)
- [ ] **Auto-generated detail/form** — detail page with activity timeline; create/edit form rendered from field definitions; relationship panels link to related standard or custom records

---

## v4.2 — SSO / OIDC

Enterprise IT won't approve a tool that requires separate credentials. OIDC first; SAML on request.

### AuthService Changes
- [ ] **OIDC client middleware** — add `AddOpenIdConnect` to AuthService; configurable per-tenant `IssuerUrl`, `ClientId`, `ClientSecret` stored in a new `SsoConfig` table; supports any standards-compliant IdP (Azure AD, Okta, Google Workspace, Auth0)
- [ ] **JIT provisioning** — on first successful OIDC callback, look up the user by email; if not found, create a new `User` and publish `UserRegistered`; if found, update display name and issue a JWT
- [ ] **Tenant SSO config endpoints** — `GET/PUT /api/tenants/{id}/sso` (Admin only) to configure IdP settings; test-connection endpoint validates the OIDC discovery document
- [ ] **SAML 2.0 (optional/later)** — SAML support via ITfoxtec.Identity.Saml2; activate when a tenant SSO config has `Protocol = Saml2`; share the JIT provisioning path with OIDC

### Frontend
- [ ] **"Sign in with SSO" button** — login page checks `GET /api/auth/sso-available?domain={emailDomain}` and shows the SSO button if the tenant has SSO configured; button initiates the OIDC redirect
- [ ] **Tenant SSO settings page** — admin section; form for issuer URL, client ID, client secret, attribute mapping (email, display name, role claim); "Test connection" button; enable/disable toggle

---

## v4.3 — AI Features

Targeted AI capabilities that reduce manual work — grounded in CRM data to minimize hallucination risk.

### AI Integration Service (new thin service or middleware)
- [ ] **Claude API client** — a shared `AiService` with a single `CompleteAsync(systemPrompt, userPrompt)` method wrapping the Anthropic SDK; configurable model, max tokens, and temperature via `appsettings`
- [ ] **Account brief generation** — `POST /api/accounts/{id}/ai/brief` — assembles account fields + last 20 activities + open deals into a prompt; returns a 3–5 sentence summary of relationship status, key contacts, and next steps; cached for 1 hour
- [ ] **Deal brief generation** — `POST /api/deals/{id}/ai/brief` — deal stage, value, associated contacts, recent activities → deal health summary and suggested next action
- [ ] **Activity summarization** — `POST /api/contacts/{id}/ai/summary` — last 30 days of activity timeline → concise relationship summary for quick context before a call or meeting

### Natural Language Search
- [ ] **NL query endpoint** — `POST /api/search/nl` accepts a natural language query string; sends a prompt to Claude with the entity schema and filter capabilities; Claude returns a structured filter object; endpoint executes the filter and returns results; falls back to keyword search if classification fails
- [ ] **Query examples** — seed the prompt with examples: "deals over $50k not updated in 30 days" → `{ entity: deals, filters: [{ field: value, op: gt, value: 50000 }, { field: updatedAt, op: lt, value: -30d }] }`

### Frontend
- [ ] **"Generate Summary" button** — appears on Account, Deal, and Contact detail pages; clicking calls the brief endpoint and renders the result in a card with a "Regenerate" option and timestamp
- [ ] **AI search input** — separate tab or toggle in the global search overlay (v3.1); placeholder "Ask a question or describe what you're looking for…"; results shown with the interpreted filter displayed so users can verify
- [ ] **Smart task suggestions** — on Deal detail page, a collapsible "Suggested next steps" section calls the deal brief and extracts action items as pre-filled task create buttons

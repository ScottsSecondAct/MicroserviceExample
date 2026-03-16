
# Roadmap

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

## v2.1 — Enterprise User Management

Replaces the current open self-registration model with an admin-controlled identity system suitable for enterprise deployment. Builds on the CRM-specific roles added in v2.0.

### Current Gaps
The system currently allows anyone to self-register and is automatically assigned the `Member` role. There is no admin-managed provisioning, no invite flow, no password reset, no account deactivation, and no SSO. The `Unassigned` role exists in the enum but is never used — it was always intended as a holding state pending admin approval.

### Account Provisioning

Two options; pick one based on deployment target:

**Option A — Admin-managed accounts (simpler, self-hosted)**
- [x] **Disable public self-registration** — `POST /api/registration/register` gated behind Admin-only authorization policy
- [x] **Invite flow** — Admin calls `POST /api/users/invite` with an email address; AuthService generates a crypto-secure, time-limited token (48h default) and sends an invite email; recipient sets their password via `POST /api/registration/accept-invite`; token is single-use
- [ ] **`Unassigned` holding state** — newly invited users start as `Unassigned`; Admin explicitly promotes them to `Member`, `SalesRep`, or `Manager` before they can access CRM data

**Option B — SSO / Identity Provider (recommended for enterprise)**
- [ ] **OIDC integration** — add `AddOpenIdConnect` to AuthService; support Okta, Azure AD, or Google Workspace as the upstream IdP; users authenticate through the corporate IdP and never set a password in this system
- [ ] **JIT provisioning** — on first successful OIDC callback, AuthService creates an `Unassigned` user record and a UserManagement profile automatically; Admin promotes the role before the user can proceed
- [ ] **Disable password-based login** — when SSO is enabled, the username/password login path is removed entirely; AuthService becomes a thin OIDC relay + JWT minter

### Role & Account Administration
- [ ] **Admin: user list** — `GET /api/users` (Admin only) returning all users with email, display name, role, and active status; exposed in frontend Admin section (see v1.6)
- [ ] **Admin: role assignment** — `PATCH /api/users/{id}/role` (Admin only); replaces the current automatic `Member` assignment on registration
- [ ] **Admin: deactivate / reactivate** — `PATCH /api/users/{id}/status`; deactivated users are rejected at JWT validation time in the gateway; soft-delete, not hard-delete
- [ ] **Admin: resend invite** — re-issue a fresh invite token for a pending user

### Password Management (Option A only)
- [ ] **Forgot password flow** — `POST /api/auth/forgot-password` generates a signed reset token; `POST /api/auth/reset-password` consumes it; tokens are single-use and expire in 1 hour
- [ ] **Force password change on first login** — invite-accepted users are flagged `MustChangePassword`; AuthService returns a specific claim that the frontend detects and redirects to a change-password page before allowing further navigation
- [ ] **Password policy** — minimum length, complexity rules enforced at AuthService; policy configurable via `appsettings.json`

### Audit Trail (identity events)
- [ ] **Identity audit log** — record who performed admin actions (invite sent, role changed, account deactivated) with timestamp and actor UserId; stored in UserManagementService; accessible via `GET /api/users/audit` (Admin only)

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

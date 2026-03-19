
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

## v1.1 — Infrastructure Foundation ✅

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

## v2.0 — Hardening & Production Readiness ✅

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

## v2.2 — Username Login & Tenancy Foundation ✅

Adds username-based login and lays the structural groundwork for multi-tenancy so the username feature is built on the correct schema from day one — avoiding a breaking migration when v3.0 multi-tenancy is implemented.

Supports all three planned deployment models:
- **On-prem / dedicated cloud** — single tenant; `TenantId` is always the same value, invisible to users; username `admin` works without conflict
- **Shared cloud (SaaS)** — multiple tenants on one instance; tenant resolved from subdomain at the gateway; `(TenantId, Username)` composite uniqueness allows `admin` in every tenant

### Tenant Entity (new)
- [x] **Tenant table** — add a `Tenant` entity to `AuthDbContext` and `UserManagementDbContext`; fields: `TenantId` (PK), `Slug` (unique), `DisplayName`, `CreatedAt`
- [x] **Default tenant seed** — single-tenant deployments seed one tenant on startup via `appsettings.json` `DefaultTenant` section; invisible to users

### TenantId on Users and Profiles
- [x] **`TenantId` FK on `AuthService.User`** — single-tenant deployments always use the seeded default; no UI exposure needed
- [x] **`TenantId` FK on `UserManagementService.UserProfile`** — same pattern

### Username Login
- [x] **`Username` field on `User`** — nullable string with composite `(TenantId, Username)` unique constraint; replaces any global unique index
- [x] **Admin seed** — default admin gets `Username = "admin"` (or value from `DefaultAdmin` config)
- [x] **Registration** — username auto-derived from email prefix (e.g. `john.doe@corp.com` → `john.doe`); numeric suffix appended on collision within tenant
- [x] **`LoginRequest.EmailOrUsername`** — rename `Email` field; drop `[EmailAddress]` validation; `LoginService` branches on `@` to look up by email or by `(TenantId, Username)`
- [x] **Forgot-password stays email-only** — email is still required to send a reset link

### Admin Provisioning Flow
- [x] **Startup seed** preserved for single-tenant deployments (on-prem / dedicated cloud) where `DefaultTenant` + `DefaultAdmin` config is present
- [x] **Provisioning endpoint** — `POST /api/tenants/provision` (bootstrap-secret auth) for shared cloud tenant creation; creates tenant + first admin atomically

### Gateway Update
- [x] **Subdomain → `X-Tenant-Id`** — YARP middleware extracts subdomain from `Host` header and forwards `X-Tenant-Id` to downstream services for shared cloud deployments; single-tenant deployments fall back to the default tenant

### Frontend
- [x] **Login form** — label changes to "Email or username"; `type="text"`, `autoComplete="username"`

---

## v2.3 — Security Hardening

Closes the remaining open items from SECURITY_VULNERABILITIES.md before any production deployment. Most are low-effort, one-file changes. Items 11 (mTLS), 20 (OTel exporter), and 22 (EF migrations) are already tracked under v3.0.

### Critical
- [ ] **Downstream services unauthenticated (issue 1)** — each downstream service currently performs no JWT validation and relies entirely on the gateway; add JWT Bearer middleware to all five services (`AccountService`, `ContactService`, `DealService`, `ActivityService`, `ReportingService`) and mark all controllers `[Authorize]`; the gateway already forwards the `Authorization` header, so no token re-issuance is needed
- [ ] **PBKDF2 iteration count (issue 2)** — raise `iterationCount` from `10_000` to `600_000` in `AuthService/Services/PasswordService.cs` (NIST SP 800-132 2023 minimum for PBKDF2-SHA256); add a `HashVersion` field to the `User` entity and re-hash on next successful login to migrate existing accounts transparently
- [ ] **Timing attack in password comparison (issue 3)** — replace the early-exit comparison loop in `PasswordService.cs` with `CryptographicOperations.FixedTimeEquals()`
- [ ] **RabbitMQ `?? "guest"` fallback (issue 5)** — remove the `?? "guest"` / `?? "guest"` fallbacks from every service `Program.cs`; if the `RabbitMQ:Username` or `RabbitMQ:Password` config keys are absent the service should throw a clear startup error rather than silently authenticate with default credentials
- [ ] **RabbitMQ management UI exposed (issue 6)** — remove the `"15672:15672"` port mapping from `docker-compose.yml`; the management UI should never be reachable from outside the Docker network; use an SSH tunnel for operator access if needed

### High
- [ ] **Swagger in production (issue 7)** — wrap `UseSwagger()` / `UseSwaggerUI()` in `if (app.Environment.IsDevelopment())` in all services that currently call them unconditionally (`AuthService`, `AccountService`, `ContactService`, `DealService`, `ActivityService`)
- [ ] **Containers run as root (issue 10)** — add a non-root `appuser` and a `USER appuser` directive to all six service Dockerfiles; eliminates root-inside-container privilege escalation risk

### Medium
- [ ] **Security response headers (issue 13)** — add `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, and `Referrer-Policy: strict-origin-when-cross-origin` headers in a middleware pipeline step at the API gateway
- [ ] **CORS policy too broad (issue 14)** — replace `.AllowAnyHeader().AllowAnyMethod()` in the gateway's CORS policy with `.WithHeaders("Authorization", "Content-Type").WithMethods("GET", "POST", "PUT", "DELETE")`
- [ ] **Health endpoint topology leak (issue 15)** — replace the detailed JSON health response on `GET /health` with a simple `Healthy` / `Unhealthy` string for unauthenticated callers; expose the full downstream report only to internal monitoring via a separate authenticated endpoint
- [ ] **Debug logging in production (issue 16)** — remove `builder.Logging.AddDebug()` from all service `Program.cs` files; structured Serilog output to Seq already covers all production logging needs
- [ ] **JWT issuer/audience placeholders (issue 17)** — replace the hardcoded `"https://localhost"` issuer and `"YourAppUsers"` audience in `docker-compose.yml` and `appsettings.json` with env vars (`JWT_ISSUER`, `JWT_AUDIENCE`); document in `.env.example`
- [ ] **AllowedHosts wildcard (issue 18)** — replace `"AllowedHosts": "*"` in all service `appsettings.json` files with the specific hostnames each service will receive requests on; use an env var for deployments where the hostname is not known at build time
- [ ] **Request body size limits (issue 19)** — configure `KestrelServerOptions.Limits.MaxRequestBodySize = 65_536` (64 KB) globally in all services; apply tighter per-endpoint limits via `[RequestSizeLimit]` on any endpoints that legitimately need larger payloads

### Low
- [ ] **Input length limits (issue 23)** — add `[MaxLength]` data annotations to all string fields on every entity model (`User`, `UserProfile`, `Contact`, `Account`, `Deal`, `Activity`); prevents unbounded `text` columns in PostgreSQL and closes the log-injection surface

### API & Correctness
- [ ] **API versioning strategy** — establish a versioning convention before v3.0 changes the API surface; URL path versioning (`/api/v1/contacts`) is recommended for discoverability; add an `api-version` header to all responses; document the deprecation and sunset policy so clients have a migration window when a version is retired
- [ ] **Server-side pagination audit** — audit every list endpoint across all seven services and confirm each enforces a maximum page size; any endpoint that returns an unbounded result set is a latency and memory risk; add `?page` and `?pageSize` parameters with a hard cap (e.g. 200 records) where missing
- [ ] **Dependency and image vulnerability scanning** — add a CI step that runs `dotnet list package --vulnerable` for NuGet CVEs and Trivy against all Docker images; fail the build on critical or high severity findings; configure GitHub Dependabot for automated dependency update PRs

---

## v2.4 — Feature Flags

Controlled rollout of new functionality to specific users, tenants, or percentages of traffic without a code deployment. Required before any gradual feature rollout strategy can be executed safely across subsequent versions.

- [ ] **Feature flag service** — integrate a feature flag provider; self-hosted GrowthBook or Unleash for on-prem/private cloud compatibility; configurable via env var so the implementation is swappable without touching business logic; expose a simple `IFeatureFlags.IsEnabled(flagName, context)` interface to all services
- [ ] **Flag evaluation context** — populate evaluation context from the JWT claims (`UserId`, `TenantId`, `Role`) so flags can be targeted at specific users, tenants, roles, or percentage rollouts
- [ ] **Platform admin flag management** — UI in the platform admin console to create, enable, disable, and configure targeting rules for flags without a deployment; changes take effect within seconds
- [ ] **Gradual rollout support** — percentage-based rollout (e.g. 10% of tenants) for safer feature releases; canary deployments to a named set of tenants before broad rollout

---

## v3.0 — Multi-Tenancy

Allows multiple independent organizations to share the same deployment with full data isolation. This is a significant cross-cutting refactor that touches every service.

### Prerequisites (complete before any multi-tenancy work)
- [x] **Introduce EF Core migrations** — `EnsureCreated()` cannot add columns to existing databases; all 6 services must be migrated to `dotnet ef migrations` before any schema changes can be applied reliably across environments
- [ ] **Optimistic concurrency** — add a `RowVersion` (timestamp / `xmin` in PostgreSQL) column to all CRM entities via migration; configure EF Core `IsRowVersion()` so concurrent updates to the same record return a 409 conflict rather than silently overwriting each other; controllers catch `DbUpdateConcurrencyException` and return a structured error
- [ ] **Database index strategy** — audit all FK columns and filter parameters used in repository queries (`TenantId`, `AccountId`, `ContactId`, `OwnerId`, `Status`, `Stage`, `Type`, `IsDeleted`) and add covering indexes via migration; without these every filtered list query is a full table scan once row counts grow beyond tens of thousands

### Frontend Production Deployment
The frontend currently runs only as a Vite dev server (`npm run dev`). It has no containerized production build, so the Docker Compose stack cannot serve the UI without a developer machine running the dev server separately.

- [ ] **`frontend/Dockerfile`** — two-stage build: `node` stage runs `npm ci && npm run build` to produce `dist/`; `nginx:alpine` stage copies `dist/` and serves it; nginx config sends all routes to `index.html` (required for React Router client-side routing) and can optionally proxy `/auth`, `/users`, etc. to the gateway (eliminating the Vite proxy dependency in production)
- [ ] **`frontend` service in `docker-compose.yml`** — builds from `frontend/Dockerfile`; exposes port `80`; depends on `api-gateway`
- [ ] **Gateway `AllowedOrigins` via env var** — replace the hardcoded `http://localhost:5173` with an environment variable (`ALLOWED_ORIGINS`) so the gateway can accept requests from the real production domain without a code change; document in `.env.example`
- [ ] **Caddy reverse proxy for SSL termination** — add a `caddy` service to `docker-compose.yml` in front of nginx; Caddy auto-provisions and renews Let's Encrypt certificates with no manual cert management; it terminates HTTPS on `:443` and forwards plain HTTP to nginx on `:80`; configure the public domain via a `DOMAIN` env var in `.env`

### Tenant Resolution
- [x] **Tenant identification strategy** — subdomain-based (`acme.yourapp.com`) chosen and implemented in v2.2; gateway middleware extracts the subdomain from the `Host` header and forwards `X-Tenant-Id` to downstream services
- [x] **Tenant registry** — `Tenant` table added to `AuthDbContext` and `UserManagementDbContext` in v2.2; fields: `TenantId`, `Slug`, `DisplayName`, `CreatedAt`
- [x] **Gateway tenant extraction** — YARP middleware resolves subdomain to `TenantId` and forwards as `X-Tenant-Id` header; implemented in v2.2; single-tenant deployments fall back to the default tenant

### Data Isolation (Row-Level)
Row-level isolation with a shared database is the most practical starting point — strongest isolation (DB-per-tenant) can be layered on later for high-value customers.

- [ ] **Add `TenantId` to all entities** — every CRM entity across all 6 services gains a non-nullable `TenantId` column; covered by migrations
- [ ] **`ITenantContext` service** — scoped DI service populated by middleware from the incoming tenant header; available throughout the request pipeline
- [ ] **EF global query filters** — each `DbContext` registers `.HasQueryFilter(e => e.TenantId == _tenantContext.TenantId)` on every entity; ensures all queries are automatically scoped without per-query changes
- [ ] **Write-path tenant stamping** — repositories set `TenantId` from `ITenantContext` on every created entity; enforced at the repository base class level

### Auth & Identity
- [x] **`TenantId` in JWT claims** — AuthService encodes the resolved tenant into the JWT at login time; done in v2.2; downstream services extract it from the token as a secondary verification
- [ ] **Tenant-scoped registration** — users register within a specific tenant context; cross-tenant access is not permitted
- [x] **Public self-registration disabled** — done in v2.1; each tenant's first admin is provisioned by the platform operator via `POST /api/tenants/provision`; admin invites additional users within their tenant

### Platform Super Admin
The existing `POST /api/tenants/provision` endpoint (bootstrap-secret auth, v2.2) is the seed of platform administration, but full SaaS operation requires a formalized platform-admin layer that sits above all tenants.

- [ ] **`PlatformAdmin` role** — a new top-level role encoded in the JWT that grants cross-tenant authority; distinct from a tenant's own `Admin` role, which is always scoped to one tenant; platform admins authenticate against a dedicated internal tenant or a separate credential store
- [ ] **Tenant management endpoints** — `GET /api/platform/tenants` (list all tenants with status, user count, created date), `POST /api/platform/tenants` (create tenant + first admin atomically, supersedes the bootstrap endpoint), `PUT /api/platform/tenants/{id}` (rename, update config), `DELETE /api/platform/tenants/{id}` (suspend — soft disable, not hard delete)
- [ ] **Tenant suspension enforcement** — gateway checks tenant active status on every request and returns `403 Tenant Suspended` for disabled tenants; status cached with a short TTL to avoid per-request DB calls
- [ ] **Impersonation** — `POST /api/platform/tenants/{id}/impersonate` issues a short-lived JWT scoped to the target tenant with an `impersonatedBy` claim; all audit log entries made during an impersonation session record the platform admin's identity alongside the tenant admin's; impersonation sessions are time-limited (15 minutes) and cannot be refreshed
- [ ] **Platform audit log** — records all platform-admin actions (tenant created, suspended, impersonation started/ended) with timestamp and actor; stored separately from per-tenant audit logs; accessible only to platform admins
- [ ] **Platform admin console (frontend)** — protected route visible only to `PlatformAdmin` role; tenant list with status badges, create-tenant form, suspend/reactivate toggle, impersonate button; distinct visual treatment (e.g. banner or color scheme change) to make it clear the operator is in platform-admin context
- [ ] **System health dashboard (frontend)** — a page in the platform admin console showing real-time health status for every service; polls `GET /health` on each service via the gateway and displays a colour-coded badge (Healthy / Degraded / Unhealthy) with last-checked timestamp; DLQ depth gauges per service; links out to the relevant Grafana dashboard panel for drill-down; auto-refreshes every 30 seconds

### Messaging
- [ ] **`TenantId` on all events** — add `TenantId` to `BaseEvent` in `SharedLibrary.Messaging`; all publishers set it; all consumers scope their DB operations using it
- [ ] **Consumer tenant context** — MassTransit consumers populate `ITenantContext` from the incoming message before calling any service or repository method

### Service-to-Service HTTP
- [ ] **Forward tenant header** — all inter-service HTTP clients (`AccountClient`, `ContactClient`, etc.) forward the `X-Tenant-Id` header on every outbound request; validated by the receiving service

### Testing
- [ ] **Multi-tenant integration tests** — integration tests create two tenants and assert that data created under tenant A is not visible to tenant B; covers both the HTTP API and event-consumer paths

### Infrastructure Hardening
These items are prerequisites for any serious production deployment regardless of hosting model.

- [ ] **Secrets Management Phase 2** — move JWT key, DB passwords, and RabbitMQ credentials from environment variables into a proper secret store; HashiCorp Vault for on-prem and private cloud; AWS Secrets Manager / Azure Key Vault / GCP Secret Manager for cloud deployments; services retrieve secrets at startup via the provider SDK rather than reading env vars directly
- [ ] **Automated database backups** — scheduled pg_dump (or WAL-based continuous archiving via pgBackRest / Barman) for all seven databases; encrypt backup output with a KMS-managed key (gpg or cloud provider KMS) before writing to storage so a leaked backup file does not expose plaintext data; retention policy; documented restore procedure (including decryption step) tested on every release
- [ ] **Zero-downtime deployments** — health-check-gated rolling updates so a deployment does not drop in-flight requests; in Docker Compose this means `depends_on: condition: service_healthy` and staged container replacement; in Kubernetes this is a rolling `Deployment` strategy with `readinessProbe`
- [ ] **mTLS between services** — encrypt and mutually authenticate all inter-service HTTP traffic on the Docker / Kubernetes network; in Docker Compose use a sidecar approach (Envoy or Caddy) or a service mesh; in Kubernetes use Linkerd or Istio for transparent mTLS with no application code changes (addresses SECURITY_VULNERABILITIES.md issue 11)
- [ ] **Service → PostgreSQL TLS** — add `SslMode=Require` (and `TrustServerCertificate=false` with a CA cert in production) to all seven Npgsql connection strings; configure each PostgreSQL container with `ssl_cert_file` and `ssl_key_file`; prevents plaintext DB traffic on the Docker/Kubernetes network even after mTLS is in place for HTTP traffic
- [ ] **Service → RabbitMQ AMQPS** — switch all MassTransit host configurations from `amqp://` to `amqps://`; configure RabbitMQ with a TLS certificate; encrypts all message payloads (including `UserRegistered` events containing email addresses) in transit between services and the broker
- [ ] **PII field-level encryption** — encrypt sensitive columns (email, phone, address fields in ContactService and AccountService) at the application layer before writing to the DB and decrypt on read; use AES-256-GCM with keys managed by the KMS from Secrets Management Phase 2; provides defense in depth so unrestricted DB read access does not expose plaintext PII; required for HIPAA and some GDPR interpretations; implement as an EF Core value converter so encryption is transparent to repository code
- [ ] **OTel OTLP exporter** — replace the `AddConsoleExporter()` in all services with an OTLP exporter targeting a collector (Seq already receives traces; add a configurable `OTLP_ENDPOINT` env var so the same images can ship to Jaeger, Grafana Tempo, or a cloud provider's trace backend without rebuilding) (addresses SECURITY_VULNERABILITIES.md issue 20)
- [ ] **Outbox pattern** — wrap domain event publishing and the corresponding DB write in a single atomic operation using MassTransit's built-in outbox; eliminates the silent event loss window that currently exists if a service crashes after committing to the DB but before publishing to RabbitMQ; requires EF Core migrations (already in prerequisites)
- [ ] **Idempotent consumers** — RabbitMQ guarantees at-least-once delivery; every consumer must handle duplicate message delivery gracefully; add an `InboxMessage` table per service that records processed message IDs and skips reprocessing; MassTransit's inbox/outbox implementation covers both this and the outbox pattern in one addition
- [ ] **Circuit breakers and request timeouts** — add Polly `ResiliencePipeline` policies to all typed HTTP clients (`UserRoleClient`, `AccountClient`, `ContactClient`); configure: a timeout (e.g. 3 seconds) so a slow downstream never holds a thread indefinitely, and a circuit breaker that trips after 5 consecutive failures and stops calling the downstream for a 30-second cooldown period; prevents cascading failures when a dependency is degraded rather than fully down
- [ ] **DLQ message replay tooling** — add a platform-admin endpoint `GET /api/platform/dlq` that lists messages currently in each service's dead-letter queue (name, count, sample payload) and `POST /api/platform/dlq/{queue}/replay` that moves messages back to the source queue for reprocessing; removes the need to access the RabbitMQ management UI directly for routine operational tasks
- [ ] **Distributed caching (Redis)** — add a Redis container to `docker-compose.yml` and wire it up via `IDistributedCache`; cache: role lookups in AuthService (TTL 60s, invalidated on role change), tenant active-status checks at the gateway (TTL 30s), and team list responses from UserManagementService (TTL 120s); these are the highest-frequency inter-service calls and the most straightforward to cache safely

### Tenant Onboarding
New tenants land in an empty system with no guidance. An onboarding flow reduces time-to-value and decreases early churn.

- [ ] **Onboarding checklist** — a dismissible checklist shown to the tenant Admin on first login: set your display name, invite your first teammate, create your first contact, create your first deal; each step links directly to the relevant page; progress persisted per tenant
- [ ] **Sample data option** — a one-click "Populate with sample data" action in the onboarding checklist that calls the existing seed script logic via a platform API; creates realistic demo accounts, contacts, deals, and activities so the tenant can explore a populated system before adding real data; sample data is tagged `IsSampleData = true` and can be cleared in one action
- [ ] **In-app guided tour** — a step-by-step tooltip walkthrough triggered from the onboarding checklist; covers the sidebar navigation, creating a contact, moving a deal on the pipeline board, and logging an activity; implemented client-side (e.g. Shepherd.js); skippable at any point

### Observability & Alerting
The system has `/health` endpoints, DLQ depth checks, structured logs in Seq, and OTel traces — but no persistent metrics collection, no dashboards that show trends over time, and no automated alerting when something breaks. Operators currently have no way to know a service is degraded until a user reports it.

- [ ] **Prometheus metrics** — add `prometheus-net.AspNetCore` (or OTel metrics exporter) to all services; expose a `/metrics` endpoint on each; instrument: HTTP request rate, error rate (4xx/5xx), p50/p95/p99 latency, DLQ depth (already in health checks — surface as a gauge), DB connection pool usage, MassTransit consumer lag
- [ ] **Grafana in docker-compose.yml** — add a `grafana` service pointing at Prometheus as a data source; provision dashboards as config files (no manual setup required after `docker compose up`)
- [ ] **Pre-built dashboards** — one dashboard per concern: *Service Health* (request rate, error rate, latency per service), *Infrastructure* (DB connection pool, RabbitMQ queue depths and DLQ gauges), *Business* (registration rate, login rate, deal creation rate — useful for spotting anomalies)
- [ ] **Alert rules** — define Grafana alert rules for the conditions that require immediate response: any service health check failing for > 30 seconds, error rate > 5% over a 5-minute window, p99 latency > 2s, DLQ depth > 0 for > 5 minutes, DB connection pool saturation > 80%
- [ ] **Notification channels** — configure Grafana contact points for alert delivery; support email and Slack webhook out of the box; channel configured via env vars so no credentials are committed; alert message includes service name, condition, current value, and a deep link to the relevant dashboard panel

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

## v3.9 — Production Deployment Models

Covers the infrastructure and operational work needed to ship reliably across all four deployment targets: shared cloud SaaS, dedicated cloud, private cloud, and on-premises. Items are grouped by which model introduces the requirement; most benefit multiple models.

### All Models
- [ ] **Upgrade path tooling** — a documented and scripted upgrade procedure for moving from one release to the next; EF Core migrations handle schema changes, but the operator also needs a tested sequence for stopping services, running migrations, and starting new containers without data loss; include a rollback procedure
- [ ] **Volume / disk encryption at rest** — all Docker volumes (seven PostgreSQL databases, Seq log data) must be encrypted at the storage layer; for cloud deployments enable provider-managed encryption on all disks and volumes (AWS EBS, Azure Managed Disk, GCP Persistent Disk — all transparent to the application, zero code changes); for on-prem use LUKS full-disk encryption on the host; for Kubernetes use encrypted PersistentVolumes via the storage class; a stolen disk or snapshot must not expose plaintext data
- [ ] **Load testing / performance baseline** — a k6 (or NBomber) suite that runs against the full Docker Compose stack and measures: login throughput, contact list latency under concurrent load, deal creation rate, and pipeline board response time; establish a baseline before v3.0 so regressions are detectable; run as an optional CI job on release branches and record results as build artifacts

### Shared Cloud (multi-tenant SaaS)
- [ ] **Kubernetes manifests / Helm chart** — replace docker-compose as the production deployment unit; one Helm chart with values files per environment (staging, prod); `Deployment`, `Service`, `ConfigMap`, `Secret`, `HorizontalPodAutoscaler`, and `Ingress` resources for each service
- [ ] **Database connection pooling** — add PgBouncer in front of each PostgreSQL instance in transaction-pooling mode; prevents connection exhaustion as tenant count and replica count grow; configure pool size per service via env var
- [ ] **Per-tenant resource quotas** — gateway-level rate limiting scoped to `TenantId` (in addition to the existing per-IP and per-user limits) so one high-volume tenant cannot starve others; configurable per-tenant overrides stored in the tenant registry
- [ ] **Database-per-tenant option** — for high-value customers, support provisioning a dedicated PostgreSQL instance per tenant; the connection string for each tenant's DB is stored in the tenant registry and resolved at request time via `ITenantContext`; row-level isolation (v3.0) remains the default for standard tiers
- [ ] **CDN for frontend static assets** — serve the built `dist/` files from a CDN (Cloudflare, CloudFront, Fastly) rather than directly from the nginx container; reduces latency for geographically distributed users and offloads traffic from the origin
- [ ] **GDPR / data residency** — document data retention and deletion policies per deployment model; full right-to-erasure tooling (automated per-tenant hard-delete across all services) is covered in v4.8

### Dedicated Cloud (one instance per customer)
- [ ] **Infrastructure as Code** — Terraform (or Pulumi) modules that provision a complete stack (VPC, managed Postgres, managed RabbitMQ or equivalent, container runtime, load balancer, DNS, TLS cert) for a single-tenant deployment in AWS, Azure, or GCP; parameterised by region and instance size
- [ ] **Managed services option** — document and support swapping self-hosted Postgres for RDS / Cloud SQL and self-hosted RabbitMQ for AmazonMQ / Azure Service Bus; connection string and transport configuration already driven by env vars, so this is primarily documentation and Terraform module work

### Private Cloud & On-Premises
- [ ] **LDAP / Active Directory** — native LDAP bind as an alternative identity provider for organisations that have not adopted OIDC; v4.2 SSO/OIDC covers this for environments running AD FS or Azure AD with an OIDC endpoint, but some on-prem environments require a direct LDAP bind; configurable per tenant via a new `LdapConfig` table alongside `SsoConfig`
- [ ] **Air-gapped installation** — a release artefact (tarball or OCI image bundle) containing all Docker images pre-pulled so the stack can be installed on a network with no outbound internet access; a companion `docker compose load` script replaces `docker compose pull`; no dependency on Docker Hub or any external registry at install time
- [ ] **Sysadmin documentation** — an operator guide covering: required open ports and firewall rules, minimum server specs per service, how to configure an internal SMTP relay, how to point the OTel exporter at an internal collector, and how to run backups and restores

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

---

## v4.4 — Billing & Subscriptions

Subscription plan management, seat enforcement, trial periods, and payment processing.

### Plans & Entitlements
- [ ] **Subscription plan entity** — `Plan` table: `PlanId`, `Name`, `MaxSeats`, `TrialDays`, `Features` (JSON feature flag set); tenant record carries `PlanId`, `SubscriptionStatus`, and `TrialExpiresAt`
- [ ] **Seat enforcement** — reject invite and user-creation requests when the tenant is at or above `MaxSeats` for their plan; Admin sees a seats-used / seats-available indicator on the Admin > Users page
- [ ] **Trial period** — new tenants start in a configurable trial period (`TrialDays` from the plan); gateway returns `402 Trial Expired` on all non-auth routes after expiry plus a configurable grace period; expiry warning banner shown in the frontend for the 7 days preceding expiry
- [ ] **Feature gating** — services and the gateway check the tenant's plan feature set (resolved via `ITenantContext`) before executing plan-restricted operations; returns `402 Upgrade Required` with the required plan name
- [ ] **Self-serve sign-up** — a public `POST /api/tenants/signup` endpoint (distinct from the admin-only provision endpoint) that creates a tenant, seeds the default admin account, and starts the trial; paired with a sign-up page in the frontend that collects company name, email, and password

### Payment Processing
- [ ] **Stripe integration** — Stripe Checkout for self-serve plan selection; Stripe Customer and Subscription objects created per tenant on first payment; webhook endpoint (`POST /api/billing/webhook`) consumes `invoice.paid`, `invoice.payment_failed`, and `customer.subscription.deleted` events and updates tenant subscription status accordingly
- [ ] **Plan upgrade / downgrade** — `POST /api/billing/subscription` allows a tenant Admin to change their plan; prorates the charge via Stripe; seat limits and feature flags update immediately
- [ ] **Invoice history** — `GET /api/billing/invoices` returns past invoices with amount, date, and a Stripe-hosted PDF link; displayed on a Billing page in tenant admin settings

### Platform Admin
- [ ] **Subscription overview** — platform admin console shows per-tenant plan, subscription status, seat count, next renewal date, and MRR; filterable by plan and status
- [ ] **Manual overrides** — platform admin can set a tenant's plan, extend a trial, or mark a subscription as complimentary without going through Stripe; used for enterprise deals, pilots, and partnerships

---

## v4.5 — Public API & Webhooks

Machine-to-machine access and real-time event delivery to customer-configured endpoints.

### API Keys
- [ ] **API key entity** — `ApiKey` table: `KeyId`, `TenantId`, `HashedKey`, `Name`, `Scopes`, `LastUsedAt`, `ExpiresAt`; keys are scoped to a tenant and optionally to specific resource types; the plaintext key is shown once on creation and stored as a hash
- [ ] **API key authentication** — gateway accepts `Authorization: Bearer <api-key>` in addition to JWTs; resolves tenant and a service-account identity from the key; rate-limited separately from interactive user traffic
- [ ] **Key management endpoints** — `GET/POST/DELETE /api/keys` (tenant Admin only); creation returns the plaintext key once; listing shows name, scopes, last-used, and expiry without revealing the key value
- [ ] **Key management UI** — API Keys page in tenant admin settings; create key with name and scope selection; revoke with confirmation dialog; last-used timestamp

### Webhooks
- [ ] **Webhook subscription entity** — `WebhookSubscription` table: `SubscriptionId`, `TenantId`, `Url`, `Secret`, `EventTypes` (array), `IsActive`, `CreatedAt`
- [ ] **Webhook delivery** — a `WebhookDispatchConsumer` subscribes to all domain events on RabbitMQ; for each event, looks up active subscriptions matching the tenant and event type; POSTs the payload to the configured URL with an HMAC-SHA256 signature header (`X-Webhook-Signature`) so the recipient can verify authenticity
- [ ] **Delivery retries and logs** — failed deliveries (non-2xx or timeout) retried with exponential backoff up to 5 attempts; all attempts logged in a `WebhookDeliveryLog` table with response status and latency; `GET /api/webhooks/{id}/deliveries` exposes the log to the tenant admin
- [ ] **Webhook management UI** — Webhooks page in tenant admin settings; add/edit/delete subscriptions; event type multi-select; test delivery button that sends a sample payload; delivery log per subscription

### API Documentation
- [ ] **Public API docs** — generate an OpenAPI spec from all service controllers and publish a read-only Redoc or Scalar documentation site at `/docs`; versioned alongside the API; no authentication required to browse

---

## v4.6 — Notifications

Backend for the notification bell already present in the frontend UI, plus email delivery for time-sensitive CRM events.

### Notification Service (new service)
- [ ] **`Notification` entity** — `NotificationId`, `TenantId`, `UserId`, `Type`, `Title`, `Body`, `EntityType`, `EntityId`, `IsRead`, `CreatedAt`; persisted per user in a dedicated `notificationdb`
- [ ] **Event-driven creation** — `NotificationConsumer` subscribes to domain events and creates in-app notifications for relevant users: deal assigned → notify new owner, task due tomorrow → notify assignee, contact status changed → notify contact owner
- [ ] **Notification endpoints** — `GET /api/notifications` (unread first, paginated), `POST /api/notifications/{id}/read`, `POST /api/notifications/read-all`; gateway prefix `/notifications/**`
- [ ] **Real-time delivery** — push new notifications to the browser via Server-Sent Events (`GET /api/notifications/stream`) so the bell badge updates without polling; falls back to 30-second polling if SSE is unavailable

### Preferences & Email
- [ ] **Notification preferences** — per-user, per-event-type toggles for in-app and email delivery; stored in UserManagementService; respected by the consumer before creating or sending
- [ ] **Transactional event emails** — send email for high-priority events (deal assigned, task due) using the existing MailKit infrastructure; template-based with entity name, summary, and a deep link to the record
- [ ] **Digest emails** — daily and weekly summary of CRM activity (new deals, overdue tasks, pipeline changes) for opted-in users; configurable send time per user; generated by a scheduled job in NotificationService

---

## v4.7 — White-labeling & Custom Domains

Allows tenants to present the application under their own brand.

### Custom Domains
- [ ] **Custom domain per tenant** — tenant admin configures a CNAME pointing to the platform ingress; the gateway resolves the incoming hostname to a `TenantId` (extending the subdomain logic from v2.2) and serves the correct tenant; TLS certificates for custom domains provisioned automatically via ACME/Let's Encrypt
- [ ] **Domain verification** — tenant must prove ownership by adding a DNS TXT record before the custom domain is activated; `POST /api/tenants/domain/verify` checks for the record and activates on success

### Custom Branding
- [ ] **Branding entity** — `TenantBranding` table: `TenantId`, `LogoUrl`, `PrimaryColor`, `FaviconUrl`, `AppName`; served via `GET /api/tenants/branding` (public, no auth) so the frontend can apply it before the login screen renders
- [ ] **Branding application** — frontend reads branding on load and applies: logo in sidebar and login page, primary color as the Tailwind CSS accent, app name in browser tab title and email subjects
- [ ] **Branding management UI** — Branding page in tenant admin settings; logo upload (stored in object storage), color picker, live preview panel

### Custom Email Sender
- [ ] **Per-tenant SMTP configuration** — tenant admin can configure their own SMTP credentials so invite and password-reset emails arrive from their own domain rather than the platform default; credentials stored encrypted via the KMS from Secrets Management Phase 2

---

## v4.8 — Legal & Compliance

Consent tracking, data portability, and audit tooling required by GDPR, CCPA, SOC 2, and enterprise procurement reviews.

### Terms & Consent
- [ ] **Terms of service acceptance** — record the ToS version, timestamp, and IP address each user accepted; on login, detect if the current ToS version is newer than the user's last acceptance and require re-acceptance before proceeding; `GET /api/legal/tos/current` serves the current version and effective date
- [ ] **Privacy policy consent** — version-aware tracking separate from ToS so legal can update one without forcing re-acceptance of the other
- [ ] **Consent audit log** — `GET /api/legal/consent/audit` (Admin only) lists all acceptance events with user, version, timestamp, and IP; exportable as CSV

### Data Portability & Deletion
- [ ] **Full tenant data export** — `POST /api/platform/tenants/{id}/export` triggers an async job that serialises all tenant data (users, contacts, accounts, deals, activities, audit logs) to a JSON archive; notifies the requesting admin by email when ready; archive encrypted with a one-time key before storage
- [ ] **Automated right-to-erasure** — `DELETE /api/platform/tenants/{id}` hard-deletes or anonymises all data belonging to the tenant across all seven services via a coordinated workflow; publishes a `TenantDeleted` event that each service's consumer uses to purge its own store; completion confirmed once all consumers acknowledge

### Compliance Tooling
- [ ] **Data processing agreement tracking** — record which tenants have a signed DPA on file, the version, and the signing date; surfaced in the platform admin console alongside subscription status
- [ ] **Retention policy enforcement** — configurable per-tenant retention period for activities and audit logs; a scheduled job hard-deletes records older than the retention window; platform admin sets the minimum and maximum allowed periods per plan

---

## v4.9 — Product Analytics

Usage instrumentation to understand feature adoption, identify underused areas, and surface early churn signals.

### Instrumentation
- [ ] **Event tracking** — instrument key user actions across all frontend pages (page views, feature interactions, form submissions) using a structured `track(event, properties)` call; properties include `userId`, `tenantId`, `plan`, and feature-specific metadata; no PII in event payloads
- [ ] **Analytics provider integration** — ship events to PostHog (self-hostable) or Mixpanel; provider configured via env var so the same build can point at either without code changes

### Platform Admin Analytics
- [ ] **Platform usage dashboard** — platform admin console page showing: active tenants (last 30 days), monthly active users, new signups over time, trial-to-active conversion rate, top features by usage, and tenants with no activity in 14+ days as a churn risk signal
- [ ] **Per-tenant health score** — a composite score per tenant derived from logins, records created, activities logged, and features used in the last 30 days; surfaced in the tenant list so operators can prioritise outreach for at-risk tenants
- [ ] **Feature adoption metrics** — per-feature usage rates across all tenants (what percentage have created a deal, used workflow automation, enabled SSO); used to prioritise the roadmap and identify features that need better onboarding

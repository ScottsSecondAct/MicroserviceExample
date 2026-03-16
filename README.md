# MicroserviceExample
[![Open Source](https://img.shields.io/badge/Open%20Source-Yes-green.svg)](https://github.com/ScottsSecondAct/MicroserviceExample) [![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT) ![AI Assisted](https://img.shields.io/badge/AI%20Assisted-Claude-blue?logo=anthropic) [![CI](https://github.com/ScottsSecondAct/MicroserviceExample/actions/workflows/ci.yml/badge.svg)](https://github.com/ScottsSecondAct/MicroserviceExample/actions/workflows/ci.yml) [![Staging](https://github.com/ScottsSecondAct/MicroserviceExample/actions/workflows/staging.yml/badge.svg)](https://github.com/ScottsSecondAct/MicroserviceExample/actions/workflows/staging.yml) [![Release](https://github.com/ScottsSecondAct/MicroserviceExample/actions/workflows/release.yml/badge.svg)](https://github.com/ScottsSecondAct/MicroserviceExample/actions/workflows/release.yml)

A production-patterned microservices system evolving toward a full CRM in **ASP.NET Core (.NET 9)**. Built to demonstrate real distributed system design rather than toy examples: independent services with separate databases, a YARP API gateway, async event-driven communication via RabbitMQ and MassTransit, JWT authentication, distributed tracing with OpenTelemetry, and a React frontend.

## Origin Story

This project started as something much smaller. About five years ago I hand-built a two-service authentication system — AuthService and UserManagementService — as a personal exercise in ASP.NET Core microservices. It worked, it was solid, and then it sat in a private repository.

Recently I was going through old repos looking for examples I could share with students to motivate them to build real projects and put them on GitHub. When I found the original two-service system, I decided to do something more ambitious with it: evolve it into a domain that is easy for anyone to understand but complex enough to surface the real tensions in distributed system design. A CRM fit perfectly — contacts, accounts, deals, and activities give you enough moving parts to motivate every architectural decision without burying the reader in domain complexity.

The result is this project: a full CRM built as a genuine microservices system, grown incrementally from that original authentication foundation.

## Why This Project

Most microservice tutorials show a diagram with boxes and arrows, then implement a single monolith with a split folder structure. This project implements the real thing: independently deployable services, each with its own database, an API gateway as the single entry point, and a message broker decoupling inter-service workflows.

The interesting problems are in the seams. When a user registers, AuthService and UserManagementService must agree on who owns the user's role — and they can't share a database to coordinate. The JWT must carry enough claims for `GET /me` without a round-trip, yet the canonical profile lives in a different service. Registration can't block if UserManagementService is slow — so the sync HTTP call became an async event. These are real distributed system tensions, not toy problems.

This project was developed with AI assistance (Anthropic's Claude) as a design and implementation collaborator. Architecture decisions, service boundaries, and every tradeoff were made and understood by hand. The AI accelerated the work; it didn't replace the thinking.

## Current State — v1.6 (Enterprise UI Redesign) + v2.0 Complete + v2.1 (partial)

The full CRM is operational end-to-end with a professional, enterprise-grade frontend. All v2.0 hardening items are complete. v2.1 (Enterprise User Management) is underway: self-registration is disabled, admin invite flow is live, and CRM-specific roles (SalesRep, Manager) are in place.

**v1.6 (Enterprise UI Redesign):**
- **Tailwind CSS + shadcn/ui** — full component library (Dialog, Sheet, Toast, Skeleton, Select, Combobox, Pagination, DropdownMenu) replaces hand-written CSS
- **Left sidebar layout** — fixed sidebar grouped by domain (CRM, Productivity, Insights); collapsible to icon-only mode; hamburger drawer on narrow viewports
- **Top bar** — global search, notification bell, user avatar dropdown with profile link and logout
- **Breadcrumbs** — contextual navigation on all detail and form pages
- **Slideover panels** — create/edit forms open in a right-hand Sheet; reduces context loss for power users
- **Data tables** — sortable columns, pagination with page-size selector, inline row actions (Edit/Delete), bulk select with bulk-action bar
- **Feedback & states** — toast notifications, loading skeleton screens, guided empty states with CTA, confirmation dialogs for all destructive actions
- **Dashboard** — KPI stat cards with trend indicators; interactive charts with hover tooltips and clickable segments
- **Admin section** — user list page (Admin-only); role promotion/demotion; account deactivation

**v2.0 Hardening (complete):**
- **Refresh token rotation** — login returns a JWT (2h) + an opaque refresh token stored in the AuthService DB; `POST /auth/api/login/refresh` issues a new JWT and rotates the refresh token; token invalidated on use
- **Secrets management (Phase 1)** — all credentials (JWT key, DB passwords, RabbitMQ creds) injected via environment variables; `.env.example` documents every required variable; no secrets in committed files
- **Structured logging** — consistent log fields (correlationId mapped to OTel trace ID, serviceId) across all services; JSON-formatted output; OTel trace context propagated via W3C `traceparent`
- **Dead-letter queue handling** — DLQ depth health checks across all services; MassTransit retry policies with exponential backoff on all consumers; DLQ monitoring in the Docker stack
- **Rate limiting** — per-IP and per-user limits at the YARP gateway; configurable thresholds via environment variables
- **Soft delete + audit trail** — `IsDeleted`/`DeletedAt` on all CRM entities; lightweight audit log (actor, action, timestamp) per service; hard-delete replaced with soft-delete across all CRUD endpoints
- **CRM-specific roles** — `UserRole` enum expanded to `Unassigned`, `Member`, `SalesRep`, `Manager`, `Admin`; matching authorization policies on AuthService and ApiGateway

**v2.1 Enterprise User Management (partial):**
- **Admin-only registration** — `POST /api/registration/register` gated behind Admin authorization policy; public self-registration is disabled
- **Admin invite flow** — `POST /api/users/invite` (Admin) generates a time-limited, crypto-secure token and sends an invite email; `POST /api/registration/accept-invite` (public) validates the token, creates the user, and publishes `UserRegistered`

v1.5 (Reporting & Dashboards):
- ReportingService subscribes to domain events; read-model projections for pipeline value by stage, activity counts by rep, contact funnel by status; Dashboard in the frontend.

v1.4 (Activities):
- ActivityService — full CRUD for five activity types (Call, Email, Meeting, Task, Note); all entity references (ContactId, DealId, AccountId) are optional; scheduled and completed timestamps for task tracking; publishes `ActivityLogged` on create and `TaskCompleted` when a Task is first marked complete
- Frontend: Activity timeline on Contact, Deal, and Account detail pages; Activity log quick-add form; Task list page.

v1.3 (Deals & Pipeline):
- DealService with pipeline stages, deal-contact associations with role, stage-change events, and a `ContactDeleted` consumer. Kanban board with drag-and-drop in the frontend.

v1.2 (Contacts & Accounts):
- AccountService and ContactService with full CRUD, status lifecycle, domain events, and cross-service validation.
- React Router v6, TanStack Query v5, per-domain API modules, Contact and Account pages.

v1.1 (Infrastructure Foundation):
- YARP gateway, Docker Compose, async registration via RabbitMQ/MassTransit, role duplication fix, health checks, OpenTelemetry.

**Testing:** 416 tests total — 328 unit tests, 71 integration tests, and 17 E2E tests. All passing. See [TESTING.md](TESTING.md).

See [ROADMAP.md](ROADMAP.md) for full version history and upcoming features.

## Architecture

```
  Browser (React :5173)
      │
      │  Vite dev proxy
      ▼
  ApiGateway :5000  (YARP — JWT validation, routing, CORS)
      │
      ├── /auth/**  ──────────────────────────────► AuthService :5188
      │   (PathRemovePrefix: /auth)                    │
      │                                                ├─ Register: hash password → save User
      │                                                │           → publish UserRegistered ──► RabbitMQ
      │                                                │
      │                                                └─ Login: verify password
      │                                                         → GET /api/users/{id}/role ──► UserManagementService
      │                                                         → issue JWT { UserId, Email, Role }
      │
      ├── /users/**  ─────────────────────────────► UserManagementService :5151
      │   (PathRemovePrefix: /users)                   │
      │                                                └─ Consume UserRegistered ◄── RabbitMQ
      │                                                    → create UserProfile { Role: Member }
      │
      ├── /contacts/** ──────────────────────────► ContactService :5167
      │   (PathRemovePrefix: /contacts, Auth)          │
      │                                                ├─ CRUD contacts with status lifecycle
      │                                                ├─ Validate AccountId sync ──► AccountService
      │                                                └─ Publish ContactCreated / StatusChanged / Deleted ──► RabbitMQ
      │
      ├── /accounts/** ──────────────────────────► AccountService :5243
      │   (PathRemovePrefix: /accounts, Auth)          │
      │                                                ├─ CRUD accounts with firmographics
      │                                                └─ Publish AccountCreated / AccountDeleted ──► RabbitMQ
      │
      ├── /deals/**   ──────────────────────────► DealService :5290
      │   (PathRemovePrefix: /deals, Auth)             │
      │                                                ├─ CRUD deals + deal-contact associations
      │                                                ├─ Validate AccountId sync ──► AccountService
      │                                                ├─ Validate ContactId sync ──► ContactService
      │                                                ├─ Consume ContactDeleted ◄── RabbitMQ
      │                                                └─ Publish DealCreated / StageChanged / Closed ──► RabbitMQ
      │
      ├── /pipeline/** ──────────────────────────► DealService :5290
      │   (PathRemovePrefix: /pipeline, Auth)          │
      │                                                └─ GET /api/pipeline → deals grouped by stage
      │
      └── /activities/** ────────────────────────► ActivityService
          (PathRemovePrefix: /activities, Auth)        │
                                                       ├─ CRUD activities (Call/Email/Meeting/Task/Note)
                                                       ├─ Optional ContactId / DealId / AccountId references
                                                       └─ Publish ActivityLogged / TaskCompleted ──► RabbitMQ
```

### Services

**AuthService** (HTTP :5188 / HTTPS :7043)
- Owns authentication: registration, login, and JWT issuance
- Registration saves the user and publishes a `UserRegistered` event via RabbitMQ; no longer blocks on UserManagementService
- Login fetches the current role from UserManagementService synchronously before minting the JWT
- JWT tokens carry `UserId`, `Email`, and `Role` claims; expire after 2 hours

**UserManagementService** (HTTP :5151 / HTTPS :7158)
- Owns user profiles: `UserId`, `Email`, `Role`, `DisplayName`, `CreatedAt`
- Consumes `UserRegistered` events from RabbitMQ to create profiles asynchronously
- Exposes `GET /api/users/{userId}/role` for login-time role resolution by AuthService
- `POST /api/users` remains for direct profile creation (no longer called by AuthService in the happy path)

**ApiGateway** (HTTP :5000)
- YARP reverse proxy — single entry point for all clients
- Centralizes JWT Bearer validation; downstream services don't validate tokens independently
- CORS policy applied at the gateway
- Aggregates downstream health at `/health`

**AccountService** (HTTP :5243)
- Owns companies: `Name`, `Industry`, `Size`, `Website`, address fields
- Full CRUD at `/api/accounts`; publishes `AccountCreated` and `AccountDeleted`
- Enums serialized as strings via `JsonStringEnumConverter`

**ContactService** (HTTP :5167)
- Owns contacts: `FirstName`, `LastName`, `Email`, `Phone`, `Status`, optional `AccountId` / `OwnerId`
- Status lifecycle: Lead → Prospect → Customer → Churned
- Validates `AccountId` synchronously against AccountService before creating (fail-open)
- Publishes `ContactCreated`, `ContactStatusChanged`, `ContactDeleted`
- Filterable list: `?status=`, `?ownerId=`, `?accountId=`

**DealService** (HTTP :5290)
- Owns deals: `Title`, `AccountId`, `Stage`, `Value`, `Probability`, `ExpectedCloseDate`, `OwnerId`
- Pipeline stages: Prospecting → Proposal → Negotiation → Closed Won / Closed Lost (seeded on startup)
- Deal-contact associations with role: Decision Maker, Influencer, Champion
- Validates `AccountId` and `ContactId` synchronously (fail-open on network error)
- Consumes `ContactDeleted` to remove orphaned deal-contact associations
- Publishes `DealCreated`, `DealStageChanged`, `DealClosed`
- `GET /api/pipeline` returns all stages with their deals and total value per column

**ActivityService**
- Owns activities: `Type`, `Subject`, `Notes`, optional `ContactId` / `DealId` / `AccountId` / `OwnerId`
- Activity types: Call, Email, Meeting, Task, Note
- Tasks carry `ScheduledAt` and `CompletedAt` timestamps
- Publishes `ActivityLogged` on every create; publishes `TaskCompleted` the first time a Task is marked complete
- Filterable list: `?type=`, `?contactId=`, `?dealId=`, `?accountId=`, `?ownerId=`

**SharedLibrary.Auth**
- `CreateUserProfileRequest` / `CreateUserProfileResponse` DTOs
- `UserRole` enum: `Unassigned`, `Member`, `SalesRep`, `Manager`, `Admin`

**SharedLibrary.Messaging**
- `BaseEvent` record: `CorrelationId`, `OccurredAt`, `EventType`
- `UserRegistered` event

**SharedLibrary.Accounts**
- `AccountCreated`, `AccountDeleted` events

**SharedLibrary.Contacts**
- `ContactStatus` enum: `Lead`, `Prospect`, `Customer`, `Churned`
- `ContactCreated`, `ContactStatusChanged`, `ContactDeleted` events

**SharedLibrary.Deals**
- `DealStage` enum: `Prospecting`, `Proposal`, `Negotiation`, `ClosedWon`, `ClosedLost`
- `DealContactRole` enum: `DecisionMaker`, `Influencer`, `Champion`
- `DealCreated`, `DealStageChanged`, `DealClosed` events

**SharedLibrary.Activities**
- `ActivityType` enum: `Call`, `Email`, `Meeting`, `Task`, `Note`
- `ActivityLogged`, `TaskCompleted` events

### Layered pattern (per service)

```
Controller → IService → IRepository → EF DbContext → PostgreSQL
```

Each layer is defined by an interface, enabling test doubles at any boundary.

## Build & Run

### Docker (recommended)

```sh
cp .env.example .env
# Edit .env and set JWT_SECRET to a long random string

docker compose up --build
```

Services start in dependency order (databases and RabbitMQ first). The schema is applied automatically on first startup via `EnsureCreated()`.

- API Gateway: http://localhost:5000
- RabbitMQ management UI: http://localhost:15672 (credentials from `.env`)
- Seq log & trace UI: http://localhost:5341
- Frontend: `cd frontend && npm install && npm run dev` → http://localhost:5173

A default admin account is seeded on first startup:

| Field | Value |
|---|---|
| Email | `admin@example.com` |
| Password | `Admin1234!` |

Override these via environment variables before starting: `DefaultAdmin__Email`, `DefaultAdmin__Password`.

### Common Docker operations

**Start (or rebuild after code changes):**
```sh
docker compose up --build -d
```
Builds any images whose source has changed and starts all services in dependency order. Safe to run against a running stack — only changed services are recreated.

**Reset all data:**
```sh
docker compose down -v
docker compose up --build -d
```
`down -v` stops all containers and deletes every Docker volume (all database data, RabbitMQ state). Use this when you want a completely clean slate — for example, if RabbitMQ credentials in `.env` changed and the broker is rejecting connections because its stored password no longer matches. After wiping volumes, `up --build` recreates everything from scratch including the default admin seed.

**Seed sample CRM data:**
```sh
node scripts/seed-crm.js
```
Populates the CRM with realistic demo data via the API Gateway — no direct database access required. Creates 5 accounts, 9 contacts (across all status values), 6 deals (spanning every pipeline stage), deal-contact associations with roles, and 10 activities. Useful after a fresh `down -v` or in a new environment. Credentials default to the admin account in `.env`; override with `ADMIN_EMAIL` and `ADMIN_PASSWORD` env vars.

### Observability

All eight services ship OpenTelemetry tracing and Serilog structured logging, both pointing at [Seq](https://datalust.co/seq) which runs as part of the Docker stack.

**Seq UI:** http://localhost:5341 (no login required in dev)

#### Distributed tracing

Every service instruments:
- **Incoming HTTP requests** — each request becomes a span tagged with method, route, and status code
- **Outbound HTTP calls** — calls between services (e.g. AuthService → UserManagementService on login) appear as child spans
- **W3C `traceparent` propagation** — the trace ID flows across service boundaries via the HTTP header, so a single user action that touches multiple services shows up as one connected waterfall

To see a trace in Seq:
1. Make any API call through the gateway (e.g. log in, create a contact)
2. Open Seq → **Traces** tab
3. Click the trace to expand the cross-service waterfall — you'll see each service's span with timing and any errors

#### Log–trace correlation

Serilog enriches every log event with the active `TraceId` and `SpanId`. This means:
- In the **Events** tab, every log line has a `TraceId` property
- Click a `TraceId` value to filter all log events from that request across every service that handled it
- Or switch to the **Traces** tab and click into a span to see the correlated log lines inline

#### Useful Seq filter expressions

```
# All errors across all services
@Level = 'Error'

# Logs from one service
Service = 'AuthService'

# Everything from a specific request (copy TraceId from any log line)
TraceId = 'abc123def456...'

# Slow requests (>500ms)
Elapsed > 500

# Failed HTTP requests
StatusCode >= 500
```

### Local (without Docker)

**Requirements:** .NET 9 SDK, PostgreSQL, RabbitMQ

```sh
dotnet build MicroserviceExample.sln
dotnet test MicroserviceExample.sln

# Run services (each in a separate terminal)
dotnet run --project ApiGateway/src/ApiGateway/
dotnet run --project AuthService/src/AuthService/
dotnet run --project UserManagementService/src/UserManagementService/
dotnet run --project AccountService/src/AccountService/
dotnet run --project ContactService/src/ContactService/
dotnet run --project DealService/src/DealService/
dotnet run --project ActivityService/src/ActivityService/
```

Set connection strings and JWT settings via user secrets or `appsettings.Development.json`. The `appsettings.Development.json` files in each service default to localhost ports for inter-service calls.

```sh
# Frontend
cd frontend
npm install
npm run dev    # http://localhost:5173
```

The Vite proxy routes all `/auth/*`, `/users/*`, `/contacts/*`, `/accounts/*`, `/deals/*`, `/pipeline/*`, and `/activities/*` traffic to the gateway on port 5000.

### Run a single test class

```sh
dotnet test --filter "FullyQualifiedName~LoginServiceTests" \
  AuthService/src/AuthService.Tests/AuthService.Tests.csproj
```

## API Reference

All requests go through the gateway (`http://localhost:5000`). The gateway strips the path prefix before forwarding.

### Auth endpoints (`/auth/api/...`)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `POST` | `/auth/api/registration/register` | Admin Bearer | Register a new user (admin-provisioned) |
| `POST` | `/auth/api/users/invite` | Admin Bearer | Send a time-limited invite email to a new user |
| `POST` | `/auth/api/registration/accept-invite` | — | Accept an invite and set a password |
| `POST` | `/auth/api/login/login` | — | Login and receive a JWT + refresh token |
| `POST` | `/auth/api/login/refresh` | — | Exchange a refresh token for a new JWT + rotated refresh token |
| `GET`  | `/auth/api/login/me` | Bearer | Current user from JWT claims |

**Register** `POST /auth/api/registration/register` *(requires Admin JWT)*
```json
// Request
{ "email": "user@example.com", "password": "secret123" }

// Response 200
{ "message": "User registered successfully." }
```
Profile creation happens asynchronously — UserManagementService processes the `UserRegistered` event from RabbitMQ.

**Invite** `POST /auth/api/users/invite` *(requires Admin JWT)*
```json
// Request
{ "email": "newuser@example.com" }

// Response 200
{ "message": "Invite sent to newuser@example.com." }
```
Generates a crypto-secure token (48-hour expiry by default) and sends an invite email. Token is stored in the AuthService DB and is single-use.

**Accept Invite** `POST /auth/api/registration/accept-invite`
```json
// Request
{ "token": "<invite-token>", "password": "NewPassword123!" }

// Response 200
{ "message": "Account created successfully." }
```
Validates the token (exists, not used, not expired), creates the user, and publishes `UserRegistered`. The invited user can then log in immediately.

**Login** `POST /auth/api/login/login`
```json
// Request
{ "email": "user@example.com", "password": "secret123" }

// Response 200
{ "token": "<jwt>", "refreshToken": "<opaque-refresh-token>" }
```
Role is fetched live from UserManagementService on each login. The JWT expires in 2 hours; use `POST /refresh` with the refresh token to get a new pair without re-authenticating.

**Refresh** `POST /auth/api/login/refresh`
```json
// Request
{ "refreshToken": "<opaque-refresh-token>" }

// Response 200
{ "token": "<new-jwt>", "refreshToken": "<new-refresh-token>" }
```
The old refresh token is invalidated on use (rotation). Refresh tokens are stored in the AuthService database and expire independently of the JWT.

**Me** `GET /auth/api/login/me`
```json
// Response 200
{ "userId": "<guid>", "email": "user@example.com", "role": "Member" }
```

### User Management endpoints (`/users/api/...`) — requires Bearer token

| Method | Path | Description |
|--------|------|-------------|
| `GET`  | `/users/api/users/{userId}` | Fetch full user profile |
| `GET`  | `/users/api/users/{userId}/role` | Fetch role only (used by AuthService on login) |
| `GET`  | `/users/api/users/team` | Lightweight list for owner assignment dropdowns |
| `POST` | `/users/api/users` | Create profile (internal / event consumer fallback) |

### Contact endpoints (`/contacts/api/contacts`) — requires Bearer token

| Method | Path | Description |
|--------|------|-------------|
| `GET`    | `/contacts/api/contacts` | List contacts (`?status=`, `?ownerId=`, `?accountId=`) |
| `GET`    | `/contacts/api/contacts/{id}` | Get contact by ID |
| `POST`   | `/contacts/api/contacts` | Create contact |
| `PUT`    | `/contacts/api/contacts/{id}` | Update contact (partial — only set fields are updated) |
| `DELETE` | `/contacts/api/contacts/{id}` | Delete contact |

Status values: `Lead`, `Prospect`, `Customer`, `Churned`

### Account endpoints (`/accounts/api/accounts`) — requires Bearer token

| Method | Path | Description |
|--------|------|-------------|
| `GET`    | `/accounts/api/accounts` | List all accounts |
| `GET`    | `/accounts/api/accounts/{id}` | Get account by ID |
| `POST`   | `/accounts/api/accounts` | Create account |
| `PUT`    | `/accounts/api/accounts/{id}` | Update account |
| `DELETE` | `/accounts/api/accounts/{id}` | Delete account |

Industry values: `Technology`, `Finance`, `Healthcare`, `Retail`, `Manufacturing`, `Education`, `Other`
Size values: `Small`, `Medium`, `Large`, `Enterprise`

### Deal endpoints (`/deals/api/deals`) — requires Bearer token

| Method | Path | Description |
|--------|------|-------------|
| `GET`    | `/deals/api/deals` | List deals (`?stage=`, `?accountId=`, `?ownerId=`) |
| `GET`    | `/deals/api/deals/{id}` | Get deal by ID (includes contacts) |
| `POST`   | `/deals/api/deals` | Create deal |
| `PUT`    | `/deals/api/deals/{id}` | Update deal (triggers `DealStageChanged` / `DealClosed` events) |
| `DELETE` | `/deals/api/deals/{id}` | Delete deal |
| `POST`   | `/deals/api/deals/{id}/contacts` | Add contact to deal with role |
| `DELETE` | `/deals/api/deals/{id}/contacts/{contactId}` | Remove contact from deal |

Stage values: `Prospecting`, `Proposal`, `Negotiation`, `ClosedWon`, `ClosedLost`
Contact role values: `DecisionMaker`, `Influencer`, `Champion`

### Pipeline endpoint (`/pipeline/api/pipeline`) — requires Bearer token

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/pipeline/api/pipeline` | All stages with deals and total value per column |

### Activity endpoints (`/activities/api/activities`) — requires Bearer token

| Method | Path | Description |
|--------|------|-------------|
| `GET`    | `/activities/api/activities` | List activities (`?type=`, `?contactId=`, `?dealId=`, `?accountId=`, `?ownerId=`) |
| `GET`    | `/activities/api/activities/{id}` | Get activity by ID |
| `POST`   | `/activities/api/activities` | Create activity (publishes `ActivityLogged`) |
| `PUT`    | `/activities/api/activities/{id}` | Update activity (publishes `TaskCompleted` when a Task is first completed) |
| `DELETE` | `/activities/api/activities/{id}` | Delete activity |

Type values: `Call`, `Email`, `Meeting`, `Task`, `Note`

Setting `completedAt` on a `Task` for the first time publishes a `TaskCompleted` event. Subsequent updates to `completedAt` do not re-publish.

## Testing

416 tests across 15 projects. All passing.

**Unit tests (320)** — xUnit + Moq + FluentAssertions + `RichardSzalay.MockHttp`. Cover controllers, services, repositories, HTTP clients, and MassTransit consumers. EF Core InMemory for repository tests. Test files mirror source structure under `*.Tests/` projects.

**Integration tests (57)** — `WebApplicationFactory<Program>` boots the real ASP.NET Core pipeline in-process. `Testcontainers.PostgreSql` provides a real PostgreSQL instance per test class. MassTransit test harness replaces RabbitMQ. `WireMock.Net` stubs downstream HTTP services. Each service has its own integration test project under `*.IntegrationTests/`.

**E2E tests (17)** — `EndToEnd.Tests` runs against the full Docker Compose stack. Requires `docker compose up` before running.

## Roadmap

### ✅ v1.0 — Authentication baseline
Two-service auth system with synchronous registration, JWT issuance, and React frontend.

### ✅ v1.1 — Infrastructure Foundation
YARP gateway, Docker Compose, async registration via RabbitMQ/MassTransit, role duplication fix, health checks, OpenTelemetry, SharedLibrary split.

### ✅ v1.2 — Contacts & Accounts
ContactService and AccountService with full CRUD and lifecycle state machines. React Router v6, TanStack Query v5, and per-domain API modules replace the `useState`-based frontend.

### ✅ v1.3 — Deals & Pipeline
DealService with pipeline stages, deal-contact associations, and a `ContactDeleted` consumer. Kanban board with drag-and-drop in the frontend. Integration tests for all five services.

### ✅ v1.4 — Activities
ActivityService (calls, emails, meetings, tasks, notes). Activity timeline on contact, deal, and account detail pages. Task list page. Activity log quick-add form.

### ✅ v1.5 — Reporting & Dashboards
ReportingService subscribes to domain events and builds read-model projections. Dashboard with pipeline value by stage, contact funnel, and activity counts by rep.

### ✅ v1.6 — Enterprise UI Redesign
Full frontend overhaul: Tailwind CSS + shadcn/ui component library, left sidebar layout, top bar, breadcrumbs, slideover panels, sortable/paginated data tables with bulk-select, toast notifications, skeleton screens, guided empty states, confirmation dialogs, KPI stat cards, interactive charts, and an Admin section.

### ✅ v2.0 — Hardening
All items complete: refresh token rotation, secrets management (Phase 1), structured logging, dead-letter queue handling, rate limiting, soft delete + audit trail, integration test suite, and CRM-specific roles (SalesRep, Manager).

### v2.1 — Enterprise User Management (in progress)
Admin-only registration ✅, admin invite flow ✅. Still open: `Unassigned` holding state, role assignment endpoint, account deactivation, password management, audit trail.

See [ROADMAP.md](ROADMAP.md) for detailed feature lists per version.

## Skills Demonstrated

- **ASP.NET Core**: Controller routing, dependency injection, middleware pipeline, JWT Bearer authentication, typed `HttpClient`
- **Microservice design**: Database-per-service, API gateway (YARP), async messaging, event-driven architecture, service boundary decisions, sync vs. async communication tradeoffs
- **MassTransit + RabbitMQ**: Event publishing, consumer pattern, idempotent message handling
- **Entity Framework Core**: Code-first models, PostgreSQL with Npgsql, repository pattern
- **Security**: Password hashing (PBKDF2), JWT generation and validation, claims-based identity, centralized auth at the gateway
- **Observability**: OpenTelemetry distributed tracing, W3C trace context propagation, health checks
- **Docker**: Multi-stage Dockerfiles, Docker Compose with dependency ordering, service-name DNS, environment variable configuration
- **Testing**: xUnit, Moq, FluentAssertions, EF Core InMemory (unit); `WebApplicationFactory`, Testcontainers PostgreSQL, MassTransit test harness, WireMock.Net (integration); controller, service, repository, HTTP client, and consumer layers
- **React**: React Router v6 (protected routes, nested layouts, `useNavigate`), TanStack Query v5 (`useQuery`, `useMutation`, cache invalidation), HTML5 drag-and-drop for Kanban stage transitions, per-domain API client modules, Vite dev proxy
- **Frontend design system**: Tailwind CSS utility-first styling, shadcn/ui component library (Dialog, Sheet, Toast, Skeleton, Select, Combobox, Pagination), Recharts for interactive data visualization, responsive layout with mobile sidebar drawer

## Development Process & AI Collaboration

This project was built with AI assistance (Claude) as a design partner and implementation accelerator:

- **Service boundaries**: Deciding what each service owns — especially who holds the user's role and how it flows through registration and login — was an explicit design discussion, not an ad-hoc implementation choice.
- **Sync vs. async decisions**: The decision to make login's role-fetch synchronous (caller can't proceed without it) but registration async (profile creation is a downstream side-effect) reflects a deliberate rule applied consistently across the architecture.
- **Shared library design**: The tradeoff between a monolithic shared library (tight compile-time coupling) and topic packages (only reference what you consume) was resolved by splitting into `SharedLibrary.Auth` and `SharedLibrary.Messaging`.
- **Test architecture**: Testing each layer independently via interfaces, and using EF Core InMemory rather than mocking DbContext directly, came from reasoning about what each test should actually verify.

Every line was reviewed and understood before integration.

## License

MIT — Copyright (c) 2026 Scott Davis

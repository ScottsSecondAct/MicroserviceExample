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

- [ ] **Fix role duplication** — remove `Role` from `AuthService.User`; on login, fetch current role from UserManagementService synchronously before minting the JWT
- [ ] **Async registration** — convert `RegistrationService` from a synchronous HTTP call to publishing a `UserRegistered` event; UserManagementService becomes a consumer instead of being called directly
- [ ] **RabbitMQ + MassTransit** — add to Docker Compose; establish event publishing/consuming conventions (CorrelationId, OccurredAt, EventType base fields)
- [ ] **YARP API Gateway** — single entry point for all services; centralize JWT validation, routing, and CORS; update Vite proxy to target gateway only
- [ ] **Docker Compose** — containerize both services and PostgreSQL; service-name-based DNS replaces hardcoded `ServiceUrls` config
- [ ] **Health checks** — `AddHealthChecks()` with DB and RabbitMQ probes on all services; gateway aggregates downstream health
- [ ] **OpenTelemetry** — distributed tracing across services; W3C `traceparent` header propagation through HTTP calls and message headers
- [ ] **Split SharedLibrary** — break into topic packages (`SharedLibrary.Auth`, `SharedLibrary.Messaging`) so a change to one domain's events doesn't force a rebuild of unrelated services

---

## v1.2 — Contacts & Accounts

Core CRM entities. The building blocks every other CRM feature depends on.

- [ ] **ContactService** — full CRUD; status lifecycle (Lead → Prospect → Customer → Churned); owner assignment; validates AccountId against AccountService on create/update; publishes `ContactCreated`, `ContactStatusChanged`, `ContactDeleted`
- [ ] **AccountService** — full CRUD; firmographic fields (industry, size, website, address); publishes `AccountCreated`, `AccountDeleted`
- [ ] **SharedLibrary.Contacts / SharedLibrary.Accounts** — event packages for new services
- [ ] **UserManagementService: team endpoint** — `GET /api/users/team` returning lightweight projections (UserId, DisplayName, Role) for owner assignment dropdowns
- [ ] **Gateway routes** — `/api/contacts/**` and `/api/accounts/**`
- [ ] **Frontend: React Router** — replace `useState`-based page switching with React Router v6
- [ ] **Frontend: React Query** — replace manual fetch calls with TanStack Query for caching and background refetching
- [ ] **Frontend: per-domain API client modules** — replace flat `api.js` with `contacts.api.js`, `accounts.api.js`, etc., all sharing a common `apiClient` base that attaches the JWT header
- [ ] **Frontend: Contact module** — list (search + filter by status/owner), detail page, create/edit form
- [ ] **Frontend: Account module** — list, detail page (with associated contacts), create/edit form

---

## v1.3 — Deals & Pipeline

The sales pipeline — the primary daily-use feature for sales reps.

- [ ] **DealService** — pipeline stages (seeded: Prospecting, Proposal, Negotiation, Closed Won, Closed Lost); deal CRUD; deal-contact associations with role (Decision Maker, Influencer, Champion); validates ContactId and AccountId on create; publishes `DealCreated`, `DealStageChanged`, `DealClosed`
- [ ] **SharedLibrary.Deals** — event package
- [ ] **Gateway routes** — `/api/deals/**` and `/api/pipeline/**`
- [ ] **DealService subscribes to `ContactDeleted`** — handle deals whose associated contact is removed
- [ ] **Frontend: Pipeline board** — Kanban view grouped by stage; drag-and-drop stage updates
- [ ] **Frontend: Deal detail** — associated contacts, account, value, probability, expected close date, activity timeline stub
- [ ] **Frontend: Deal create/edit form** — stage selector, contact/account association

---

## v1.4 — Activities

The activity log ties contacts, deals, and reps together into a complete interaction history.

- [ ] **ActivityService** — activity types: Call, Email, Meeting, Task, Note; references ContactId, DealId, AccountId (all optional); scheduled/completed timestamps for tasks; publishes `ActivityLogged`, `TaskCompleted`
- [ ] **SharedLibrary.Activities** — event package
- [ ] **Gateway routes** — `/api/activities/**`
- [ ] **Frontend: Activity log form** — quick-add accessible from Contact, Deal, and Account detail pages
- [ ] **Frontend: Activity timeline** — chronological feed on Contact and Deal detail pages
- [ ] **Frontend: Task list** — all incomplete tasks assigned to the current user

---

## v1.5 — Reporting & Dashboards

Visibility into pipeline health and rep activity, powered by an event-driven read model.

- [ ] **ReportingService** — subscribes to `DealCreated`, `DealStageChanged`, `DealClosed`, `ActivityLogged`, `ContactStatusChanged`; maintains denormalized projections (pipeline value by stage, activity counts by rep, contact funnel by status); no external write API
- [ ] **Gateway routes** — `/api/reports/**`
- [ ] **Frontend: Dashboard** — pipeline summary chart (value by stage), activity counts per rep, recent contacts and deals

Dashboard data will lag source services by seconds due to the event-driven projection model. This is acceptable for all reporting use cases.

---

## v2.0 — Hardening & Production Readiness

- [ ] **Refresh tokens** — 2-hour JWT expiry with no refresh mechanism logs users out mid-session; implement refresh token rotation in AuthService
- [ ] **Secrets management** — move JWT key and connection strings out of `appsettings.json`; Phase 1: environment variables via Docker Compose; Phase 2: Vault or cloud secret store
- [ ] **Structured logging** — JSON-formatted logs with consistent fields (correlationId, userId, serviceId) across all services; ship to a central log aggregator
- [ ] **Dead-letter queue handling** — monitoring and alerting on DLQ depth for all RabbitMQ queues; MassTransit retry policies with exponential backoff on all consumers
- [ ] **Rate limiting** — at the gateway; per-IP and per-user limits
- [ ] **Soft delete + audit trail** — `IsDeleted`/`DeletedAt` on all CRM entities; lightweight audit log (who changed what and when) per service
- [ ] **Integration test suite** — end-to-end tests covering registration, contact creation, and deal creation across live services in a Docker Compose test environment
- [ ] **CRM-specific roles** — expand `UserRole` beyond `Member`/`Admin` to include `SalesRep` and `Manager`; migrate enum out of SharedLibrary into UserManagementService's own domain

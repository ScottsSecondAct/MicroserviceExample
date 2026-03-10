# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build entire solution
dotnet build MicroserviceExample.sln

# Run all tests
dotnet test MicroserviceExample.sln

# Run tests for a single service
dotnet test AuthService/src/AuthService.Tests/AuthService.Tests.csproj
dotnet test UserManagementService/src/UserManagementService.Tests/UserManagementService.Tests.csproj

# Run a single test
dotnet test --filter "FullyQualifiedName~<TestClassName>" AuthService/src/AuthService.Tests/AuthService.Tests.csproj

# Run a service locally
dotnet run --project AuthService/src/AuthService/
dotnet run --project UserManagementService/src/UserManagementService/
```

## Architecture

Two ASP.NET Core (.NET 9) microservices with synchronous HTTP inter-service communication, each backed by its own PostgreSQL database via Entity Framework Core.

### Services

**AuthService** (HTTP :5188 / HTTPS :7043)
- Handles registration, login, and JWT token issuance
- On registration, calls UserManagementService to create a profile and retrieve the assigned role
- JWT tokens include claims: Email, UserId, Role (2-hour expiry)

**UserManagementService** (HTTP :5151 / HTTPS :7158)
- Manages user profiles (UserId, Email, Role, DisplayName, CreatedAt)
- Called synchronously by AuthService via `HttpClientFactory`; base URL configured in `appsettings.json` as `ServiceUrls:UserManagementService`

**SharedLibrary**
- Contains shared DTOs (`CreateUserProfileRequest`, `CreateUserProfileResponse`) and the `UserRole` enum (Unassigned, Member, Admin)
- No external dependencies; referenced by both services

### Layered pattern (per service)

Controllers → Services (interfaces) → Repositories (interfaces) → EF DbContext

### Registration flow
1. `POST /api/registration/register` → AuthService validates uniqueness, hashes password, saves user
2. AuthService calls `POST /api/users` on UserManagementService with `CreateUserProfileRequest`
3. UserManagementService creates profile with `Role=Member`, returns `CreateUserProfileResponse`
4. AuthService stores the role and returns success

### Testing
- xUnit + Moq + FluentAssertions
- `Microsoft.EntityFrameworkCore.InMemory` used for repository/DbContext testing
- Test files mirror source structure under `*.Tests/` projects

## CRM Evolution Plan

### New Services

| Service | Owns | Communicates |
|---|---|---|
| **ContactService** | Contacts, status lifecycle (Lead→Customer), owner assignment | Validates AccountId via sync HTTP to AccountService; publishes `ContactCreated`, `ContactStatusChanged` |
| **AccountService** | Companies, firmographics, addresses | Publishes `AccountCreated`, `AccountDeleted` |
| **DealService** | Pipeline stages, deals, deal-contact associations | Validates ContactId/AccountId sync; publishes `DealStageChanged`, `DealClosed` |
| **ActivityService** | Calls, emails, meetings, tasks, notes | Publishes `ActivityLogged`, `TaskCompleted` |
| **ReportingService** | Read-model projections only (pipeline value, activity counts) | Subscribes to events from all above; no write API |
| **YARP Gateway** | JWT validation, routing, CORS, rate limiting | Infrastructure — no business logic |

### Changes to Existing Services

**AuthService:** Remove `Role` from the `User` entity (duplicated from UserManagementService). On login, fetch current role from UserManagementService synchronously, encode it in the JWT. Convert registration from a synchronous HTTP call to publishing a `UserRegistered` event.

**UserManagementService:** Add a role-lookup endpoint (for login-time resolution), a `GET /api/users/team` lightweight projection (for assignment dropdowns in the frontend), and become a consumer of `UserRegistered` instead of being called directly.

### Sync vs Async Decision Rule

**Use sync HTTP** when the caller can't proceed without the result — login fetching a role, ContactService validating an AccountId before creating a contact.

**Use async messaging (RabbitMQ + MassTransit)** when the effect is a downstream side-effect — UserManagementService creating a profile after registration, ReportingService updating pipeline totals after a deal closes.

### SharedLibrary Evolution

Split into topic packages: `SharedLibrary.Auth`, `SharedLibrary.Contacts`, `SharedLibrary.Deals`, etc. A single change currently forces a rebuild of everything; topic packages mean services only reference the events they consume.

### Frontend

Needs React Router (current `useState`-based switching won't scale), React Query for server state caching, and per-domain API client modules. New pages: Contact list/detail, Account list/detail, Deal pipeline board (Kanban), Activity timeline, Dashboard.

### Phased Roadmap

**Phase 1 — Infrastructure Foundation** *(prerequisite for all CRM work)*
Fix role duplication bug, add Docker Compose, add YARP gateway, convert registration to async via RabbitMQ.

**Phase 2 — Contacts & Accounts**
ContactService + AccountService with full CRUD and lifecycle. Update frontend with React Router and React Query.

**Phase 3 — Deals**
DealService with pipeline stages and deal-contact associations. Kanban board in the frontend.

**Phase 4 — Activities**
ActivityService (all types). Activity timeline on Contact and Deal detail pages.

**Phase 5 — Reporting**
ReportingService subscribes to domain events and builds read-model projections. Dashboard with pipeline and activity charts.

**Phase 6 — Hardening**
Refresh tokens, structured logging with correlation IDs, dead-letter queue monitoring, rate limiting, soft-delete + audit trail, integration test suite.

## Potential Improvements

- **Async messaging** — Registration is tightly coupled: if UserManagementService is down, registration fails. A message broker (RabbitMQ, Kafka) would decouple them and improve resilience.
- **API Gateway** — Clients hit each service directly. A gateway (YARP, Ocelot) would provide a single entry point, centralize routing, and handle auth token validation instead of each service doing it independently.
- **Centralized secrets** — JWT key and DB connection strings live in `appsettings.json`. A secrets manager (Vault, AWS Secrets Manager, Azure Key Vault) or environment variable injection would be more production-appropriate.
- **Health checks** — No `/health` endpoints. ASP.NET Core's built-in `AddHealthChecks()` is needed for container orchestration (Kubernetes liveness/readiness probes).
- **Distributed tracing** — Cross-service calls have no trace context. OpenTelemetry would allow tracing a registration request across both services.
- **Docker / docker-compose** — No containerization. A `docker-compose.yml` with both services and PostgreSQL would make local development self-contained.
- **Role ownership** — AuthService stores `Role` on its own `User` entity, duplicating data that UserManagementService owns. This creates a potential inconsistency; role should be fetched from UserManagementService rather than cached in AuthService's DB.

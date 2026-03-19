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
dotnet test AccountService/src/AccountService.Tests/AccountService.Tests.csproj
dotnet test ContactService/src/ContactService.Tests/ContactService.Tests.csproj
dotnet test DealService/src/DealService.Tests/DealService.Tests.csproj
dotnet test ActivityService/src/ActivityService.Tests/ActivityService.Tests.csproj
dotnet test ReportingService/src/ReportingService.Tests/ReportingService.Tests.csproj

# Run integration tests for a single service
dotnet test AuthService/src/AuthService.IntegrationTests/AuthService.IntegrationTests.csproj
dotnet test DealService/src/DealService.IntegrationTests/DealService.IntegrationTests.csproj

# Run a single test class
dotnet test --filter "FullyQualifiedName~<TestClassName>" AuthService/src/AuthService.Tests/AuthService.Tests.csproj

# Run a service locally (each in its own terminal)
dotnet run --project ApiGateway/src/ApiGateway/
dotnet run --project AuthService/src/AuthService/
dotnet run --project UserManagementService/src/UserManagementService/
dotnet run --project AccountService/src/AccountService/
dotnet run --project ContactService/src/ContactService/
dotnet run --project DealService/src/DealService/
dotnet run --project ActivityService/src/ActivityService/
dotnet run --project ReportingService/src/ReportingService/

# Docker (recommended for running the full stack)
cp .env.example .env   # set JWT_SECRET and other vars
docker compose up --build -d

# Frontend
cd frontend && npm install && npm run dev   # http://localhost:5173
```

## Architecture

Eight ASP.NET Core (.NET 9) microservices behind a YARP API gateway. Services communicate via synchronous HTTP (when the caller can't proceed without the result) or asynchronous messaging via RabbitMQ + MassTransit (for downstream side-effects). Each service owns its own PostgreSQL database — no shared data stores.

### Services

**ApiGateway** (HTTP :5000)
- YARP reverse proxy — single entry point for all clients
- Validates JWT Bearer tokens; downstream services do not independently validate tokens
- Applies CORS, rate limiting, and routes all traffic by path prefix
- Subdomain → `X-Tenant-Id` forwarding for shared-cloud multi-tenancy

**AuthService** (HTTP :5188 / HTTPS :7043 | Docker: :8080)
- Handles login, admin-provisioned registration, invite flow, password reset
- On registration: publishes `UserRegistered` event to RabbitMQ (async)
- On login: fetches current role from UserManagementService synchronously via `IUserRoleClient`
- JWT tokens carry `UserId`, `Email`, `Role`, `TenantId` claims (2-hour expiry)
- Issues opaque refresh tokens stored in `authdb`; `POST /api/login/refresh` rotates them

**UserManagementService** (HTTP :5151 / HTTPS :7158 | Docker: :8080)
- Owns user profiles: `UserId`, `TenantId`, `Username`, `Email`, `Role`, `DisplayName`, `IsActive`, `CreatedAt`
- Consumes `UserRegistered` → creates profile with `Role=Unassigned`
- `GET /api/users/{userId}/role` used by AuthService at login
- `GET /api/users/team` lightweight projection for owner dropdowns
- Admin endpoints: list users, assign role, deactivate/reactivate, resend invite, audit log

**AccountService** (Docker: :8080)
- Full CRUD for company accounts; publishes `AccountCreated`, `AccountDeleted`

**ContactService** (Docker: :8080)
- Full CRUD for contacts; status lifecycle: Lead → Prospect → Customer → Churned
- Validates `AccountId` synchronously against AccountService (fail-open)
- Publishes `ContactCreated`, `ContactStatusChanged`, `ContactDeleted`

**DealService** (Docker: :8080)
- Full CRUD for deals + deal-contact associations with role
- Pipeline stages seeded on startup: Prospecting, Proposal, Negotiation, Closed Won, Closed Lost
- Validates `AccountId` and `ContactId` synchronously (fail-open)
- Consumes `ContactDeleted` → removes orphaned deal-contact associations
- Publishes `DealCreated`, `DealStageChanged`, `DealClosed`

**ActivityService** (Docker: :8080)
- Full CRUD for activities: Call, Email, Meeting, Task, Note
- All entity references (`ContactId`, `DealId`, `AccountId`, `OwnerId`) are optional
- Publishes `ActivityLogged` on create; `TaskCompleted` when a Task is first marked complete

**ReportingService** (Docker: :8080)
- Read-only. No write API. Maintains event-driven projections.
- Consumes `DealCreated`, `DealStageChanged`, `DealClosed`, `ActivityLogged`, `ContactStatusChanged`
- Endpoints: `GET /api/reports/pipeline|activities|contacts|dashboard`

### Shared Libraries

| Package | Contents |
|---|---|
| `SharedLibrary.Auth` | `UserRole` enum (`Unassigned`, `Member`, `SalesRep`, `Manager`, `Admin`), Auth DTOs |
| `SharedLibrary.Messaging` | `BaseEvent` record (`CorrelationId`, `OccurredAt`, `EventType`), `UserRegistered` |
| `SharedLibrary.Accounts` | `AccountCreated`, `AccountDeleted` events |
| `SharedLibrary.Contacts` | `ContactStatus` enum, `ContactCreated`, `ContactStatusChanged`, `ContactDeleted` events |
| `SharedLibrary.Deals` | `DealStage` enum, `DealContactRole` enum, `DealCreated`, `DealStageChanged`, `DealClosed` events |
| `SharedLibrary.Activities` | `ActivityType` enum, `ActivityLogged`, `TaskCompleted` events |

### Layered pattern (per service)

```
Controllers → Services (interfaces) → Repositories (interfaces) → EF DbContext
```

Each layer is defined by an interface, enabling test doubles at any boundary.

### Registration flow (current — async)
1. `POST /auth/api/registration/register` (Admin only) → AuthService validates, hashes password, saves `User`, publishes `UserRegistered` to RabbitMQ
2. UserManagementService consumes `UserRegistered` → creates `UserProfile` with `Role=Unassigned`
3. Admin promotes the user's role via `PATCH /api/admin/users/{id}/role`

### Login flow
1. `POST /auth/api/login/login` → AuthService verifies credentials
2. AuthService calls `GET /api/users/{userId}/role` on UserManagementService (synchronous)
3. JWT minted with `UserId`, `Email`, `Role` claims; refresh token stored in DB

## Key Patterns

**ServiceResult pattern:**
```csharp
ServiceResult.Success(data, message, statusCode)
ServiceResult.Failure(message, statusCode)
```
Controllers call `StatusCode(result.StatusCode, result.Data ?? result.Message)`.

**MassTransit publish:**
```csharp
await _publishEndpoint.Publish(new SomeEvent { ... });
```

**MassTransit consume:** implement `IConsumer<T>` in `ServiceName/Consumers/`.

**HTTP validation clients** (AccountClient, ContactClient): fail-open on network exceptions — a downstream outage does not block the creating service.

**EF Core:** no migrations; all services use `EnsureCreated()` on startup. Use `Include()` in repositories for navigation properties (no lazy loading). Repository tests use `Guid.NewGuid().ToString()` as the in-memory DB name for isolation.

## Testing

- **Unit:** xUnit + Moq + FluentAssertions + `RichardSzalay.MockHttp`. EF Core InMemory for repository tests.
- **Integration:** `WebApplicationFactory<Program>` + `Testcontainers.PostgreSql` + `AddMassTransitTestHarness` + `WireMock.Net` for downstream stubs.
- **E2E:** `EndToEnd.Tests` — requires `docker compose up` first.

**Integration test notes:**
- `public partial class Program { }` required at end of each service's `Program.cs`
- Set connection string via `builder.UseSetting("ConnectionStrings:<Name>", ...)` before `ConfigureServices`
- Use `_harness.Bus.Publish()` (not a scoped `IPublishEndpoint`) when publishing from integration tests
- WireMock stubs for AuthService UMS role must return JSON `{ userId, role: 1 }`, not a plain string
- `Consumer_DealStageChanged_MovesDealBetweenStages` in `ReportingService.IntegrationTests` is a known flaky test (passes in isolation, occasionally fails in parallel runs)

**Registration returns 409** (not 400) on duplicate email.

# Testing Strategy

This document describes the three-layer testing strategy for this project: unit, integration, and end-to-end. It covers what exists, what's missing, and the recommended order of implementation.

---

## Guiding Principles

- **Test at the right layer.** Unit tests verify logic in isolation. Integration tests verify a service's HTTP pipeline and database behavior end-to-end. E2E tests verify cross-service flows through the gateway.
- **Lower layers catch regressions cheaply.** A bug caught by a unit test costs seconds to diagnose. The same bug caught by an E2E test costs minutes. Build the lower layers first.
- **Integration tests pay off most when written alongside new services**, not retrofitted later. Establish the pattern once; adapt it per service.
- **E2E tests are most valuable when the feature set tells a complete story.** Defer the E2E infrastructure until the domain is rich enough to make the scenarios meaningful.

---

## Current State

Fifteen test projects, 603 tests (unit + integration) + 20 E2E tests. All layers complete.

| Component | Unit | Integration | E2E |
|-----------|------|-------------|-----|
| AuthService — services | ✅ | ✅ | ✅ |
| AuthService — controllers | ✅ RegistrationController, LoginController | ✅ | ✅ |
| AuthService — repository | ✅ | ✅ | ✅ |
| AuthService — UserRoleClient | ✅ | ✅ | ✅ |
| UserManagementService — services | ✅ | ✅ | ✅ |
| UserManagementService — consumer | ✅ | ✅ | ✅ |
| UserManagementService — controller | ✅ | ✅ | ✅ |
| UserManagementService — repository | ✅ | ✅ | ✅ |
| ContactService — services | ✅ | ✅ | ✅ |
| ContactService — controller | ✅ | ✅ | ✅ |
| ContactService — repository | ✅ | ✅ | ✅ |
| ContactService — AccountClient | ✅ | ✅ | ✅ |
| AccountService — services | ✅ | ✅ | ✅ |
| AccountService — controller | ✅ | ✅ | ✅ |
| AccountService — repository | ✅ | ✅ | ✅ |
| DealService — services | ✅ | ✅ | ✅ |
| DealService — controllers | ✅ | ✅ | ✅ |
| DealService — repository | ✅ | ✅ | ✅ |
| DealService — AccountClient | ✅ | ✅ | ✅ |
| DealService — ContactClient | ✅ | ✅ | ✅ |
| DealService — ContactDeletedConsumer | ✅ | ✅ | ✅ |
| ActivityService — services | ✅ | ✅ | ✅ |
| ActivityService — controller | ✅ | ✅ | ✅ |
| ActivityService — repository | ✅ | ✅ | ✅ |
| ReportingService — consumers | ✅ | ✅ | ✅ |
| ReportingService — controller | ✅ | ✅ | ✅ |

**623 tests total. 479 unit + 124 integration + 20 E2E. All passing.**
**E2E tests in EndToEnd.Tests require Docker Compose stack (`docker compose up --build -d`).**

### Coverage (last measured: March 2026, pre-v2.1 completion)

| Metric | Coverage |
|--------|----------|
| Line | 96.2% |
| Branch | 80.7% |
| Method | 98.6% |

Generated with `dotnet test --collect:"XPlat Code Coverage"` and `reportgenerator`. Excludes `*.Tests`, `*.IntegrationTests`, and `EndToEnd.Tests` assemblies. Coverage figures are from before the v2.1 feature additions; a fresh coverage run is recommended.

### Unit test count by project

| Project | Tests |
|---------|-------|
| AuthService.Tests | 132 |
| UserManagementService.Tests | 86 |
| ContactService.Tests | 48 |
| AccountService.Tests | 41 |
| DealService.Tests | 79 |
| ActivityService.Tests | 50 |
| ReportingService.Tests | 43 |
| **Total** | **479** |

### Integration test count by project

| Project | Tests |
|---------|-------|
| AuthService.IntegrationTests | 17 |
| UserManagementService.IntegrationTests | 23 |
| AccountService.IntegrationTests | 15 |
| ContactService.IntegrationTests | 15 |
| DealService.IntegrationTests | 19 |
| ActivityService.IntegrationTests | 17 |
| ReportingService.IntegrationTests | 18 |
| **Total** | **124** |

---

## Layer 1 — Unit Tests ✅ Complete

**Stack:** xUnit + Moq + FluentAssertions + `RichardSzalay.MockHttp`

Unit tests verify a single class in isolation. All dependencies are mocked. No database, no network, no message broker.

### What was added

**Controllers** — mock the service interface, assert the correct `IActionResult` type and status code for success, not-found, validation failure, service failure (null-data `??` branch), and exception paths (service throws → 500).

```
AccountService.Tests/Controllers/
  AccountsControllerTests.cs     — 15 tests: GetAll, GetById, Create (name validation),
                                   Update, Delete; success + service-failure + 404 + 500 paths

ContactService.Tests/Controllers/
  ContactsControllerTests.cs     — 14 tests: GetAll (filter pass-through verified),
                                   GetById, Create (firstName/lastName/email validation),
                                   Update, Delete

UserManagementService.Tests/Controllers/
  UsersControllerTests.cs        — 10 tests: CreateUserProfile (email validation),
                                   GetUserProfile, GetTeam, GetUserRole

ActivityService.Tests/Controllers/
  ActivitiesControllerTests.cs   — 16 tests: GetAll, GetById (found + 404), Create
                                   (valid + empty subject), Update, Delete (found + 404);
                                   service-failure paths (null-data ?? message branch);
                                   exception paths (service throws → 500)

DealService.Tests/Controllers/
  DealsControllerTests.cs        — exception-path tests (service throws → 500) for
                                   GetAll, GetById, Create, Update, Delete, AddContact,
                                   RemoveContact

DealService.Tests/Controllers/
  PipelineControllerTests.cs     — 3 tests: GetBoard returns 200, empty repository,
                                   repository throws → 500

ReportingService.Tests/Controllers/
  ReportsControllerTests.cs      — disposed-context exception tests for GetPipeline,
                                   GetActivities, GetContacts, GetDashboard (→ 500)
```

**Repositories** — fresh in-memory database per test via `Guid.NewGuid().ToString()` database name.

```
AuthService.Tests/Repository/
  UserRepositoryTests.cs              — 8 tests: Add, GetByEmail ×2, GetById ×2,
                                        Update, Delete, Delete-no-throw
  RefreshTokenRepositoryTests.cs      — 4 tests: AddAsync, GetByTokenAsync found +
                                        not-found, RevokeAsync sets IsRevoked

UserManagementService.Tests/Repository/
  UserProfileRepositoryTests.cs       — 9 tests: Add, GetById ×2, GetByEmail ×2,
                                        GetAll, Update, Delete, Delete-no-throw

ContactService.Tests/Repository/
  ContactRepositoryTests.cs           — 11 tests: GetAll with no filter / status filter /
                                        ownerId filter / accountId filter; GetById ×2;
                                        Add; Update; Delete; Delete-no-throw

AccountService.Tests/Repository/
  AccountRepositoryTests.cs           — 9 tests: GetAll ×2, GetById ×2, Add,
                                        Update, Delete, Delete-no-throw

ActivityService.Tests/Repository/
  ActivityRepositoryTests.cs          — 12 tests: Add + GetById, GetById not-found,
                                        GetAll no filter, GetAll by contactId/dealId/
                                        accountId/ownerId/type, Update, Delete,
                                        Delete-no-throw, GetAll ordered by createdAt desc

DealService.Tests/Repository/
  DealRepositoryTests.cs              — DealContact operations: AddDealContactAsync,
                                        RemoveDealContactAsync (found + not-found),
                                        RemoveDealContactsByContactIdAsync (with matches +
                                        no matches)
```

**Services** — mock all dependencies; verify event publishing, validation, and state transitions. All optional-field update paths (HasValue true branches) explicitly exercised.

```
ActivityService.Tests/Services/
  ActivitiesServiceTests.cs      — 14 tests: Create valid (publishes ActivityLogged),
                                   Create empty subject (no publish), Create verifies
                                   event fields, GetById found + not-found, GetAll,
                                   Update not-found, Update fields, Update all optional
                                   fields (Type/ContactId/DealId/AccountId/OwnerId/
                                   ScheduledAt), Update Task first completion (publishes
                                   TaskCompleted), Update already-completed Task (no
                                   re-publish), Update non-Task type completed (no
                                   publish), Delete found, Delete not-found

AccountService.Tests/Services/
  AccountsServiceTests.cs        — UpdateAsync all optional fields test covers Industry,
                                   Size, Website, Street, City, State, PostalCode, Country

ContactService.Tests/Services/
  ContactsServiceTests.cs        — UpdateAsync all optional fields test covers FirstName,
                                   LastName, Email, Phone, AccountId, OwnerId

DealService.Tests/Services/
  DealsServiceTests.cs           — additional tests: UpdateAsync all optional fields
                                   (AccountId/Value/Probability/ExpectedCloseDate/OwnerId),
                                   UpdateAsync with no Stage provided (no event),
                                   CreateAsync with empty title (400)
```

**Infrastructure** — DlqHealthCheck branch coverage across all three services that use it.

```
DealService.Tests/Infrastructure/DlqHealthCheckTests.cs
UserManagementService.Tests/Infrastructure/DlqHealthCheckTests.cs
ReportingService.Tests/Infrastructure/DlqHealthCheckTests.cs

  Each contains 8 tests covering:
  - Healthy: no error queues
  - Degraded: error queue with messages (includes queue name + count in data)
  - Healthy: error queue with 0 messages (ignored)
  - Degraded: management API returns non-2xx
  - Degraded: HttpRequestException (network unreachable)
  - Healthy: config keys absent (covers ?? default branches)
  - Healthy: API returns JSON "null" (covers queues == null branch)
  - Degraded: TaskCanceledException (covers catch-filter branch)
```

**Models / utilities**

```
AuthService.Tests/Services/
  ServiceResultTests.cs          — 6 tests: Success (defaults), Success (all args),
                                   Failure (defaults), Failure (custom code),
                                   Error (defaults → 500), Error (custom code)

AuthService.Tests/Models/
  RegisterResponseTests.cs       — 2 tests: property roundtrip, default values

ReportingService.Tests/Consumers/
  DealClosedConsumerTests.cs     — 1 test: Consume completes without side effects
```

**HTTP clients** — `RichardSzalay.MockHttp` mocks the `HttpMessageHandler` to test failure-handling logic without a real network.

```
AuthService.Tests/Services/
  UserRoleClientTests.cs        — 4 tests: 200 Member, 200 Admin,
                                   404→Unassigned, network exception→Unassigned

ContactService.Tests/Services/
  AccountClientTests.cs         — 3 tests: 200→true, 404→false,
                                   network exception→true (fail-open explicitly asserted)
```

---

## Layer 2 — Integration Tests ✅ Complete

**New packages:**

| Package | Purpose |
|---------|---------|
| `Microsoft.AspNetCore.Mvc.Testing` | `WebApplicationFactory<Program>` — boots the real ASP.NET Core pipeline in-process |
| `Testcontainers.PostgreSql` | Spins up a real PostgreSQL container per test class |
| `MassTransit.Testing` | In-memory bus harness — assert events published without needing RabbitMQ |
| `WireMock.Net` | Mocks downstream HTTP services (e.g., mock AccountService when testing ContactService) |

Integration tests verify that the full HTTP pipeline of a single service works correctly against a real database. A `WebApplicationFactory<Program>` boots the service with a Testcontainers PostgreSQL instance substituted for the real database, and `MassTransit.Testing` substitutes for RabbitMQ. Downstream HTTP calls (e.g., ContactService → AccountService) are stubbed with WireMock.

### Factory pattern

Each project has one factory class used as an `IClassFixture`. The factory:
1. Starts a PostgreSQL Testcontainer
2. Overrides the DbContext connection string via `builder.UseSetting()` before `ConfigureServices` (required so the health check's Npgsql registration sees the correct string)
3. Replaces MassTransit with the in-memory test harness

```csharp
public class AccountServiceFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync() => await _db.StartAsync();
    public new async Task DisposeAsync() => await _db.DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:AccountDbConnection", _db.GetConnectionString());

        builder.ConfigureServices(services =>
        {
            services.RemoveDbContext<AccountDbContext>();
            services.AddDbContext<AccountDbContext>(o =>
                o.UseNpgsql(_db.GetConnectionString()));

            services.AddMassTransitTestHarness();
        });
    }
}
```

### Key scenarios per service

**AuthService.IntegrationTests**
```
POST /api/registration/register (admin JWT)    → 200, user row in DB, UserRegistered on bus
POST /api/registration/register (duplicate)    → 409, no DB write, no event
POST /api/login/login (valid credentials)      → 200, JWT with correct sub/UserId/role claims
POST /api/login/login (wrong password)         → 401
POST /api/login/login (unknown email)          → 401
GET  /api/login/me (valid token)               → 200, claims match registration
GET  /api/login/me (no token)                  → 401
POST /api/login/refresh (valid token)          → 200, new JWT + rotated refresh token
POST /api/login/refresh (invalid token)        → 401
POST /api/login/refresh (empty token)          → 4xx
GET  /health                                   → 200 Healthy
```
Factory note: `AuthServiceFactory` seeds the default admin via `DefaultAdmin` config settings so integration tests can obtain an Admin JWT to call the register endpoint.

**UserManagementService.IntegrationTests**
```
POST /api/users                        → 201, profile in DB
POST /api/users (duplicate userId)     → 400
GET  /api/users/{id}                   → 200 with all profile fields
GET  /api/users/{id} (missing)         → 404
GET  /api/users/{id}/role              → 200 with role string
GET  /api/users/team                   → 200, list of {userId, displayName, role}
Consumer: publish UserRegistered       → profile row appears in DB
Consumer: publish UserRegistered twice → idempotent, still one row
GET  /health                           → 200 Healthy
```

**AccountService.IntegrationTests**
```
POST /api/accounts                     → 201, AccountCreated published with correct AccountId/Name
GET  /api/accounts                     → 200, list
GET  /api/accounts/{id}                → 200 with all fields
GET  /api/accounts/{id} (missing)      → 404
PUT  /api/accounts/{id}                → 200, DB row updated
PUT  /api/accounts/{id} (missing)      → 404
DELETE /api/accounts/{id}              → 204, AccountDeleted published
DELETE /api/accounts/{id} (missing)    → 404
GET  /health                           → 200 Healthy
```

**ContactService.IntegrationTests**
```
WireMock stubs AccountService:
  GET /api/accounts/{validId}    → 200
  GET /api/accounts/{invalidId}  → 404

POST /api/contacts (valid accountId)                → 201, ContactCreated published
POST /api/contacts (invalid accountId)              → 400, no DB write, no event
POST /api/contacts (no accountId)                   → 201
PUT  /api/contacts/{id} status Lead→Prospect        → 200, ContactStatusChanged published
                                                       (oldStatus=Lead, newStatus=Prospect)
PUT  /api/contacts/{id} (no status change)          → 200, no ContactStatusChanged published
DELETE /api/contacts/{id}                           → 204, ContactDeleted published
GET  /api/contacts                                  → 200, full list
GET  /api/contacts?status=Lead                      → filtered list
GET  /api/contacts?ownerId={guid}                   → filtered list
GET  /api/contacts?accountId={guid}                 → filtered list
GET  /api/contacts/{id} (missing)                   → 404
GET  /health                                        → 200 Healthy
```

**DealService.IntegrationTests**
```
WireMock stubs AccountService and ContactService for ID validation.

POST /api/deals                        → 201, DealCreated published
POST /api/deals (invalid accountId)    → 400, no event
POST /api/deals (missing title)        → 400
GET  /api/deals                        → 200, array
GET  /api/deals/{id}                   → 200 with all fields
GET  /api/deals/{id} (missing)         → 404
PUT  /api/deals/{id} (stage change)    → 200, DealStageChanged published
PUT  /api/deals/{id} (→ ClosedWon)     → 200, DealStageChanged + DealClosed published
DELETE /api/deals/{id}                 → 204
GET  /api/pipeline                     → 200, array of 5 stages
Consumer: ContactDeleted               → deal-contact associations removed
GET  /health                           → 200 Healthy
```

**ActivityService.IntegrationTests**
```
POST /api/activities                          → 201, ActivityLogged published
POST /api/activities (empty subject)          → 400
GET  /api/activities                          → 200, array
GET  /api/activities/{id}                     → 200 with all fields
GET  /api/activities/{id} (missing)           → 404
GET  /api/activities?type=Task                → filtered list, only Task type returned
PUT  /api/activities/{id}                     → 200, fields updated
PUT  /api/activities/{id} (Task + completedAt) → 200, TaskCompleted published, completedAt set
DELETE /api/activities/{id}                   → 204
DELETE /api/activities/{id} (missing)         → 404
GET  /health                                  → 200 Healthy
```

### Test isolation

- Each test class receives its own `PostgreSqlContainer` via `IClassFixture<ServiceFactory>`.
- Tests that write to the database should clean up after themselves, or — simpler — seed only what each test needs and rely on the per-class container being a fresh database.
- The MassTransit test harness publishes to an in-memory bus; assert published messages with `ITestHarness.Published.Select<EventType>()`.

---

## Layer 3 — End-to-End Tests

**New project:** `EndToEnd.Tests/`

**Packages:** `Polly` (retry/polling for async assertions)

E2E tests run against the full Docker Compose stack via the YARP gateway on `http://localhost:5000`. No `WebApplicationFactory` — these are plain HTTP calls. All services, databases, and RabbitMQ are real.

### When to implement

v1.4 is complete. The richest E2E scenario is the full sales flow: *register → create account → create contact → create deal → transition stage → log activity*. Implement as a standalone effort once the team is ready to invest in Docker Compose test orchestration, or alongside v1.5 (Reporting).

### Project structure

```
EndToEnd.Tests/
  EndToEnd.Tests.csproj
  Infrastructure/
    GatewayClient.cs          — HttpClient wrapper; stores JWT, attaches Bearer header;
                                LoginAsAdminAsync() logs in as the seeded admin
                                (reads DEFAULT_ADMIN_PASSWORD env var, default Admin1234!)
    RetryHelper.cs            — polls a condition until true or timeout
  Flows/
    AuthFlowTests.cs
    AccountContactFlowTests.cs
    DealFlowTests.cs
    ActivityFlowTests.cs
    ReportingFlowTests.cs
    GatewayAuthTests.cs
```

### RetryHelper (for async assertions)

The registration→profile flow is event-driven: AuthService publishes `UserRegistered`, UserManagementService consumes it asynchronously. Polling avoids brittle `Task.Delay` waits:

```csharp
public static async Task WaitUntilAsync(
    Func<Task<bool>> condition,
    TimeSpan? timeout = null,
    TimeSpan? interval = null)
{
    var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
    while (DateTime.UtcNow < deadline)
    {
        if (await condition()) return;
        await Task.Delay(interval ?? TimeSpan.FromMilliseconds(300));
    }
    throw new TimeoutException("Condition not met within timeout.");
}
```

### Key scenarios

**AuthFlowTests**
```
Register new user (admin-provisioned)
  → Login as seeded admin → GET admin JWT
  → POST /auth/api/registration/register (admin JWT) → 200
  → Poll GET /users/api/users/{userId} until 200 (async consumer lag)
  → Profile exists and role is "Member"

Login
  → Admin registers a user, user logs in
  → POST /auth/api/login/login → 200, token returned
  → GET /auth/api/login/me → claims match registration email and role

Duplicate registration
  → Admin registers same email twice → second returns 409
```

**AccountContactFlowTests**
```
Full lifecycle
  → Login as admin (seeded admin credentials)
  → POST /accounts/api/accounts → 201, capture accountId
  → POST /contacts/api/contacts (with accountId) → 201, capture contactId
  → GET /accounts/api/accounts/{accountId} → contacts section includes contactId
  → PUT /contacts/api/contacts/{contactId} { status: "Prospect" } → 200
  → GET /contacts/api/contacts/{contactId} → status is "Prospect"
  → DELETE /accounts/api/accounts/{accountId} → 204

Invalid account reference
  → POST /contacts/api/contacts with random accountId → 400
```

**DealFlowTests**
```
Pipeline lifecycle
  → Login as admin, create account + contact
  → POST /deals/api/deals → 201, capture dealId
  → POST /deals/api/deals/{dealId}/contacts → 201
  → PUT /deals/api/deals/{dealId} { stage: "ClosedWon" } → 200
  → GET /pipeline/api/pipeline → ClosedWon column includes deal
```

**ActivityFlowTests**
```
Log and complete a task
  → Login as admin, create contact
  → POST /activities/api/activities { type: "Task", subject: "Follow up", contactId } → 201
  → GET /activities/api/activities?contactId={id}&type=Task → task appears, completedAt null
  → PUT /activities/api/activities/{id} { completedAt: <now> } → 200
  → GET /activities/api/activities/{id} → completedAt set

Activity timeline per entity
  → Log Call, Email, Note against a deal
  → GET /activities/api/activities?dealId={id} → returns all three, ordered by createdAt desc
```

**GatewayAuthTests**
```
Unauthenticated access to protected routes
  → GET /contacts/api/contacts (no token)        → 401
  → GET /accounts/api/accounts (no token)        → 401
  → GET /activities/api/activities (no token)    → 401

Auth-gated and public routes
  → POST /auth/api/registration/register (no token) → 401 (Admin-only)
  → POST /auth/api/login/login                      → 200 or 400 (not 401, public)

Gateway health
  → GET /health → 200, all downstream services report Healthy
```

### Running E2E tests

The `scripts/run-e2e.sh` script handles the full lifecycle: start the stack, wait for the gateway health check, run the suite, tear down.

```sh
# Requires Docker and the .NET 9 SDK.
# Copy .env.example → .env and fill in secrets, or let the script use safe test defaults.
./scripts/run-e2e.sh
```

To run steps manually:

```sh
# Start the full stack
docker compose up --build -d

# Wait for gateway health
until curl -sf http://localhost:5000/health; do sleep 2; done

# Run E2E suite
dotnet test EndToEnd.Tests/EndToEnd.Tests.csproj

# Tear down
docker compose down -v
```

In CI, call `scripts/run-e2e.sh` after a build step. All required environment variables have safe defaults so no secrets are needed for a local smoke run.

---

## Recommended Implementation Order

### ✅ Done before v1.3 (unit gap-fill)

1. Controller tests — AccountsController, ContactsController, UsersController
2. Repository tests — all four repositories using EF Core InMemory
3. HTTP client tests — UserRoleClient, AccountClient using MockHttp

### ✅ Done alongside v1.3 (integration tests, all services through DealService)

All five integration test projects complete: AccountService, ContactService, DealService, AuthService, and UserManagementService. Each uses the `WebApplicationFactory` + Testcontainers + MassTransit harness pattern. WireMock stubs downstream HTTP calls where needed.

### ✅ Done alongside v1.5 (ReportingService unit + integration tests)

ReportingService.Tests (12 unit tests: DealCreatedConsumer, DealStageChangedConsumer, ActivityLoggedConsumer, ContactStatusChangedConsumer) and ReportingService.IntegrationTests (9 integration tests: all 4 GET endpoints, 4 consumer event flows, health check). Controller unit tests omitted — the controller has no logic beyond querying the DB, which is fully covered by integration tests.

### ✅ Done alongside v1.4 (ActivityService unit + integration tests)

ActivityService.Tests (30 unit tests: services, controller, repository) and ActivityService.IntegrationTests (11 integration tests: full CRUD, type filtering, task completion event, health check).

### ✅ Done alongside v1.4 (E2E infrastructure)

The `EndToEnd.Tests` project and Docker Compose test orchestration. Scenarios span six services via the YARP gateway. Run with `docker compose up --build -d` then `dotnet test EndToEnd.Tests/EndToEnd.Tests.csproj`.

### ✅ Done during v2.1 (Enterprise User Management tests)

AuthService and UserManagementService test suites grew substantially with v2.1 features:

- **AuthService.Tests** grew from 55 → 132: invite flow, accept-invite, forgot/reset password, force-change-on-first-login, password policy enforcement, MailKit email service, PasswordResetTokenRepository, InviteTokenRepository
- **UserManagementService.Tests** grew from 48 → 86: admin user list, role assignment, deactivate/reactivate, resend invite, identity audit log (service + repository + controller)
- **AuthService.IntegrationTests** grew from 11 → 17: forgot password, reset password, force-change redirect, invite acceptance, admin endpoints
- **UserManagementService.IntegrationTests** grew from 9 → 23: role assignment, status change, audit log retrieval, resend invite, team endpoint
- **E2E tests** grew from 17 → 20: full invite flow, deactivation enforcement, password reset flow

### ✅ Done during v1.6 (branch coverage push to ≥80%)

A targeted coverage pass raised branch coverage from 70.9% → **80.7%**. Key findings and additions:

**What Coverlet counts as branches (not try/catch):**
- `??` null-coalescing (2 branches per operator: left non-null / left null)
- `?.` null-conditional (2 branches: null / non-null)
- `HasValue` checks on nullable types (2 branches: true / false)
- `&&` / `||` short-circuit operators
- `when` filters on `catch` clauses (2 branches: filter true / false)

**Tests added:**

| Area | What was missing | Tests added |
|------|-----------------|-------------|
| Controller service-failure paths | `result.Data ?? result.Message` null-data branch uncovered in GetAll/Create actions that had no failure test | AccountsController (2), ActivitiesController (3) |
| Controller exception paths | No tests for service throwing → StatusCode 500 | ActivitiesController (5), DealsController (7), ReportsController (4) |
| PipelineController | No tests at all | PipelineControllerTests (3) |
| ActivityRepository filters | `dealId`, `accountId`, `ownerId` HasValue branches uncovered | 3 filter tests |
| DealRepository DealContact ops | `RemoveDealContactAsync` and `RemoveDealContactsByContactIdAsync` not-found/empty branches | 3 tests |
| DlqHealthCheck (3 services) | `??` null-config defaults, `queues == null` path, `TaskCanceledException` catch filter | 3 tests × 3 services = 9 tests |
| Service optional-field updates | `HasValue` true branches for each optional field in UpdateAsync methods | 1 test each for ActivityService, AccountService, ContactService, DealService |
| Zero-coverage classes | `ServiceResult.Error()`, `RegisterResponse`, `DealClosedConsumer`, `RefreshTokenRepository` | 13 tests across 4 new files |
| AuthService refresh integration | No integration test for `/api/login/refresh` endpoint | 3 tests |
| ReportingService DealClosed integration | No test verifying DealClosed consumer is a no-op | 1 test |

---

### ✅ Done during v2.2 (Username Login & Tenancy Foundation)

**Unit tests (AuthService.Tests)**
- `LoginService`: email login (contains `@`), username login (no `@`), unknown username → 401, username wrong tenant → 401
- `UserRepository`: `GetByUsernameAsync` found / not-found
- Tenant seed: default tenant created on startup, existing tenant not duplicated

**Unit tests (UserManagementService.Tests)**
- `UserProfileRepository`: composite `(TenantId, Username)` uniqueness enforced; duplicate username in same tenant → conflict; same username in different tenant → allowed

**Integration tests (AuthService.IntegrationTests)**
- `POST /api/login/login` with username → 200, correct claims
- `POST /api/login/login` with email → still works (backward compatible)
- `POST /api/login/login` with unknown username → 401

**Integration tests (UserManagementService.IntegrationTests)**
- User created with auto-derived username from email prefix
- Username collision within tenant → numeric suffix appended

**E2E tests (EndToEnd.Tests)**
- Login as admin using `admin` (username) instead of `admin@example.com`
- Register user → verify username derived from email; login with that username succeeds

---

## E2E Coverage Audit (as of v2.2)

Audit conducted March 2026 against all 7 services + gateway. 49 unique API endpoints identified; 16 E2E test methods in 6 test files.

### Coverage by service

| Service | Endpoints | Covered | % |
|---------|-----------|---------|---|
| AuthService | 10 | 3 | 30% |
| UserManagementService | 11 | 1 | 9% |
| AccountService | 5 | 2 | 40% |
| ContactService | 5 | 5 | 100% |
| DealService | 8 | 6 | 75% |
| ActivityService | 5 | 4 | 80% |
| ReportingService | 4 | 4 | 100% |
| Gateway | 1 | 1 | 100% |
| **Total** | **49** | **26** | **53%** |

### What is covered

| Endpoint | Test |
|----------|------|
| POST /auth/api/login/login | AuthFlowTests, GatewayAuthTests |
| GET /auth/api/login/me | AuthFlowTests |
| POST /auth/api/registration/register | AuthFlowTests (200 + 409) |
| GET /users/api/users/{id} | AuthFlowTests (event-driven poll) |
| POST /accounts/api/accounts | AccountContactFlowTests |
| DELETE /accounts/api/accounts/{id} | AccountContactFlowTests |
| GET /contacts/api/contacts | AccountContactFlowTests, GatewayAuthTests |
| GET /contacts/api/contacts/{id} | AccountContactFlowTests |
| POST /contacts/api/contacts | AccountContactFlowTests, DealFlowTests, ActivityFlowTests, ReportingFlowTests |
| PUT /contacts/api/contacts/{id} | AccountContactFlowTests, ReportingFlowTests |
| GET /deals/api/deals/{id} | DealFlowTests |
| POST /deals/api/deals | DealFlowTests, ReportingFlowTests, ActivityFlowTests |
| PUT /deals/api/deals/{id} | DealFlowTests |
| POST /deals/api/deals/{id}/contacts | DealFlowTests |
| GET /pipeline/api/pipeline | DealFlowTests |
| GET /activities/api/activities | ActivityFlowTests (contactId + dealId filters), GatewayAuthTests |
| GET /activities/api/activities/{id} | ActivityFlowTests |
| POST /activities/api/activities | ActivityFlowTests |
| PUT /activities/api/activities/{id} | ActivityFlowTests |
| GET /reports/api/reports/pipeline | ReportingFlowTests, DealFlowTests |
| GET /reports/api/reports/contacts | ReportingFlowTests |
| GET /reports/api/reports/dashboard | ReportingFlowTests |
| GET /health | GatewayAuthTests |

### Async event flows covered

| Event | Test |
|-------|------|
| UserRegistered → UserManagementService creates profile | AuthFlowTests.Register_CreatesUserProfile_ViaEventAsync |
| ContactStatusChanged → ReportingService updates funnel | ReportingFlowTests.ContactStatusChanged_EventuallyAppearsInContactFunnel |
| DealCreated → ReportingService updates pipeline | ReportingFlowTests.DealCreated_EventuallyAppearsInPipelineProjection |

### Gaps — High Priority

These test cross-service event correctness that unit/integration tests cannot fully validate:

1. **ContactDeleted → DealService cleanup** — `DELETE /contacts/api/contacts/{id}` is untested end-to-end; the `ContactDeletedConsumer` in DealService removes deal-contact associations, but no E2E test verifies the cascade.
2. **ActivityLogged → ReportingService** — `GET /reports/api/reports/activities` is never called directly; the activity projection is only seen indirectly through the dashboard.
3. **TaskCompleted fires exactly once** — only fires when Type==Task and completedAt is first set; no E2E validates this business rule end-to-end.
4. **DealStageChanged → ReportingService pipeline counts** — a deal moving between stages should shift the pipeline projection; no E2E test verifies this specific event flow.

### Gaps — Medium Priority (CRUD)

| Missing scenario | Endpoint |
|-----------------|----------|
| Update account | PUT /accounts/api/accounts/{id} |
| Get account by ID | GET /accounts/api/accounts/{id} |
| List accounts | GET /accounts/api/accounts |
| List deals | GET /deals/api/deals |
| Delete deal | DELETE /deals/api/deals/{id} |
| Remove contact from deal | DELETE /deals/api/deals/{id}/contacts/{contactId} |
| Delete activity | DELETE /activities/api/activities/{id} |
| Delete contact | DELETE /contacts/api/contacts/{id} |
| Get team list | GET /users/api/users/team |
| Get user role | GET /users/api/users/{id}/role |
| Activities report | GET /reports/api/reports/activities |

### Gaps — Lower Priority (auth/admin flows)

- `POST /auth/api/login/refresh` — refresh token endpoint has zero E2E coverage
- Admin user management: list users, change role, activate/deactivate (`GET|PUT /api/admin/users/...`)
- Invite workflow: `POST /users/api/users/invite` → `POST /auth/api/registration/accept-invite`
- Password management: forgot-password, reset-password, change-password

---

## Package Reference

| Package | Layer | Purpose |
|---------|-------|---------|
| `xUnit` | Unit, Integration | Test framework |
| `Moq` | Unit | Interface mocking |
| `FluentAssertions` | Unit, Integration | Readable assertions |
| `Microsoft.EntityFrameworkCore.InMemory` | Unit | In-memory DB for repository tests |
| `RichardSzalay.MockHttp` | Unit | Mock HttpMessageHandler for typed HTTP clients |
| `Microsoft.AspNetCore.Mvc.Testing` | Integration | WebApplicationFactory — real ASP.NET Core pipeline in-process |
| `Testcontainers.PostgreSql` | Integration | Real PostgreSQL container per test class |
| `MassTransit.Testing` | Integration | In-memory bus harness for event assertions |
| `WireMock.Net` | Integration | Stub downstream HTTP services |
| `Polly` | E2E | Retry/polling for async event assertions |

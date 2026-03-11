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

Thirteen test projects, 262 tests (unit + integration) + 8 E2E tests. All layers complete.

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

**306 tests total. 222 unit + 67 integration + 17 E2E. All passing.**
**E2E tests in EndToEnd.Tests require Docker Compose stack (`docker compose up --build -d`).**

### Unit test count by project

| Project | Tests | Files |
|---------|-------|-------|
| AuthService.Tests | 33 | 6 |
| UserManagementService.Tests | 26 | 5 |
| ContactService.Tests | 39 | 5 |
| AccountService.Tests | 30 | 4 |
| DealService.Tests | 46 | 7 |
| ActivityService.Tests | 30 | 3 |
| ReportingService.Tests | 18 | 5 |
| **Total** | **222** | **35** |

### Integration test count by project

| Project | Tests |
|---------|-------|
| AuthService.IntegrationTests | 8 |
| UserManagementService.IntegrationTests | 9 |
| AccountService.IntegrationTests | 9 |
| ContactService.IntegrationTests | 9 |
| DealService.IntegrationTests | 12 |
| ActivityService.IntegrationTests | 11 |
| ReportingService.IntegrationTests | 9 |
| **Total** | **67** |

---

## Layer 1 — Unit Tests ✅ Complete

**Stack:** xUnit + Moq + FluentAssertions + `RichardSzalay.MockHttp`

Unit tests verify a single class in isolation. All dependencies are mocked. No database, no network, no message broker.

### What was added

**Controllers** — mock the service interface, assert the correct `IActionResult` type and status code for success, not-found, validation failure, and exception paths.

```
AccountService.Tests/Controllers/
  AccountsControllerTests.cs     — 12 tests: GetAll, GetById, Create (name validation),
                                   Update, Delete; success + 404 + 500 paths

ContactService.Tests/Controllers/
  ContactsControllerTests.cs     — 14 tests: GetAll (filter pass-through verified),
                                   GetById, Create (firstName/lastName/email validation),
                                   Update, Delete

UserManagementService.Tests/Controllers/
  UsersControllerTests.cs        — 10 tests: CreateUserProfile (email validation),
                                   GetUserProfile, GetTeam, GetUserRole

ActivityService.Tests/Controllers/
  ActivitiesControllerTests.cs   — 8 tests: GetAll, GetById (found + 404), Create
                                   (valid + empty subject), Update, Delete (found + 404)
```

**Repositories** — fresh in-memory database per test via `Guid.NewGuid().ToString()` database name.

```
AuthService.Tests/Repository/
  UserRepositoryTests.cs              — 8 tests: Add, GetByEmail ×2, GetById ×2,
                                        Update, Delete, Delete-no-throw

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
  ActivityRepositoryTests.cs          — 9 tests: Add + GetById, GetById not-found,
                                        GetAll no filter, GetAll by contactId,
                                        GetAll by type, Update, Delete, Delete-no-throw,
                                        GetAll ordered by createdAt desc
```

**Services** — mock all dependencies; verify event publishing, validation, and state transitions.

```
ActivityService.Tests/Services/
  ActivitiesServiceTests.cs      — 13 tests: Create valid (publishes ActivityLogged),
                                   Create empty subject (no publish), Create verifies
                                   event fields, GetById found + not-found, GetAll,
                                   Update not-found, Update fields, Update Task first
                                   completion (publishes TaskCompleted), Update already-
                                   completed Task (no re-publish), Update non-Task type
                                   completed (no publish), Delete found, Delete not-found
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
POST /api/registration/register                → 200, user row in DB, UserRegistered on bus
POST /api/registration/register (duplicate)    → 409, no DB write, no event
POST /api/login/login (valid credentials)      → 200, JWT with correct sub/UserId/role claims
POST /api/login/login (wrong password)         → 401
POST /api/login/login (unknown email)          → 401
GET  /api/login/me (valid token)               → 200, claims match registration
GET  /api/login/me (no token)                  → 401
GET  /health                                   → 200 Healthy
```

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
    GatewayClient.cs          — HttpClient wrapper; stores JWT, attaches Bearer header
    RetryHelper.cs            — polls a condition until true or timeout
  Flows/
    AuthFlowTests.cs
    AccountContactFlowTests.cs
    DealFlowTests.cs
    ActivityFlowTests.cs
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
Register new user
  → POST /auth/api/registration/register → 200
  → Poll GET /users/api/users/{userId} until 200 (async consumer lag)
  → Profile exists and role is "Member"

Login
  → POST /auth/api/login/login → 200, token returned
  → GET /auth/api/login/me → claims match registration email and role

Duplicate registration
  → Register same email twice → second returns 409
```

**AccountContactFlowTests**
```
Full lifecycle
  → Register + login
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
  → Register + login, create account + contact
  → POST /deals/api/deals → 201, capture dealId
  → POST /deals/api/deals/{dealId}/contacts → 201
  → PUT /deals/api/deals/{dealId} { stage: "ClosedWon" } → 200
  → GET /pipeline/api/pipeline → ClosedWon column includes deal
```

**ActivityFlowTests**
```
Log and complete a task
  → Register + login, create contact
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

Public routes accessible without token
  → POST /auth/api/registration/register         → 200 (not 401)
  → POST /auth/api/login/login                   → 200 or 400 (not 401)

Gateway health
  → GET /health → 200, all downstream services report Healthy
```

### Running E2E tests

```sh
# Start the full stack
docker compose up --build -d

# Wait for gateway health (simple poll script)
until curl -sf http://localhost:5000/health; do sleep 2; done

# Run E2E suite
dotnet test EndToEnd.Tests/EndToEnd.Tests.csproj

# Tear down
docker compose down -v
```

In CI, add a `test-e2e` job to the GitHub Actions release workflow that runs after the build job.

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

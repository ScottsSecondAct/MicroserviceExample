# Testing Strategy

This document describes the three-layer testing strategy for this project: unit, integration, and end-to-end.

---

## Guiding Principles

- **Test at the right layer.** Unit tests verify logic in isolation. Integration tests verify a service's HTTP pipeline and database behavior end-to-end. E2E tests verify cross-service flows through the gateway.
- **Lower layers catch regressions cheaply.** A bug caught by a unit test costs seconds to diagnose. The same bug caught by an E2E test costs minutes.
- **Integration tests are most valuable when written alongside new services.** Establish the pattern once; adapt it per service.
- **E2E tests are most valuable when the feature set tells a complete story.** Scenarios should span multiple services and cover async event flows.

---

## Current State

733 tests total: 563 unit + 121 integration + 49 E2E. Unit and integration all passing. E2E requires the Docker Compose stack.

| Component | Unit | Integration | E2E |
|-----------|------|-------------|-----|
| AuthService — services | ✅ | ✅ | ✅ |
| AuthService — controllers | ✅ | ✅ | ✅ |
| AuthService — repository | ✅ | ✅ | ✅ |
| AuthService — UserRoleClient | ✅ | ✅ | ✅ |
| UserManagementService — services | ✅ | ✅ | ✅ |
| UserManagementService — consumers | ✅ | ✅ | ✅ |
| UserManagementService — controllers | ✅ | ✅ | ✅ |
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

### Coverage

| Metric | Coverage |
|--------|----------|
| Line | 97.2% |
| Branch | 82.7% |
| Method | 99.1% |

Generated with `dotnet test --collect:"XPlat Code Coverage"` + `reportgenerator`. Excludes `*.Tests`, `*.IntegrationTests`, and `EndToEnd.Tests` assemblies, and filters out `*Migrations*` and `*DbContextFactory*`.

### Unit test count by project

| Project | Tests |
|---------|-------|
| AuthService.Tests | 184 |
| UserManagementService.Tests | 93 |
| DealService.Tests | 92 |
| ContactService.Tests | 54 |
| ActivityService.Tests | 51 |
| AccountService.Tests | 45 |
| ReportingService.Tests | 44 |
| **Total** | **563** |

### Integration test count by project

| Project | Tests |
|---------|-------|
| UserManagementService.IntegrationTests | 22 |
| DealService.IntegrationTests | 19 |
| ReportingService.IntegrationTests | 18 |
| AuthService.IntegrationTests | 17 |
| AccountService.IntegrationTests | 15 |
| ActivityService.IntegrationTests | 15 |
| ContactService.IntegrationTests | 15 |
| **Total** | **121** |

---

## Layer 1 — Unit Tests

**Stack:** xUnit + Moq + FluentAssertions + `RichardSzalay.MockHttp`

Unit tests verify a single class in isolation. All dependencies are mocked. No database, no network, no message broker.

### Controllers

Mock the service interface; assert the correct `IActionResult` type and status code for success, not-found, validation failure, service failure (null-data `??` branch), and exception paths (service throws → 500).

### Repositories

Fresh in-memory database per test via `Guid.NewGuid().ToString()` database name. Tests cover Add, GetById (found + not-found), GetAll with each supported filter, Update, and Delete (found + no-throw on missing).

### Services

Mock all dependencies; verify event publishing, validation, and state transitions. All optional-field update paths (`HasValue` true and false branches) are explicitly exercised.

### Infrastructure — DlqHealthCheck

Each of the three services that include `DlqHealthCheck` (DealService, UserManagementService, ReportingService) has 8 dedicated tests covering:

- Healthy: no error queues
- Degraded: error queue with messages (name + count in data)
- Healthy: error queue with 0 messages (ignored)
- Degraded: management API returns non-2xx
- Degraded: `HttpRequestException` (network unreachable)
- Healthy: config keys absent (covers `??` default branches)
- Healthy: API returns JSON `"null"` (covers `queues == null` branch)
- Degraded: `TaskCanceledException` (covers `when` catch-filter branch)

### HTTP clients

`RichardSzalay.MockHttp` mocks the `HttpMessageHandler` to test failure-handling logic without a real network.

- **UserRoleClient** — 200 Member, 200 Admin, 404 → Unassigned, network exception → Unassigned
- **AccountClient** (ContactService) — 200 → true, 404 → false, network exception → true (fail-open)

### Branch coverage notes

Coverlet counts the following as branches (not try/catch):

- `??` null-coalescing (2 branches per operator)
- `?.` null-conditional (2 branches)
- `HasValue` checks on nullable types (2 branches)
- `&&` / `||` short-circuit operators
- `when` filters on `catch` clauses (2 branches)

---

## Layer 2 — Integration Tests

**Stack:** `Microsoft.AspNetCore.Mvc.Testing` + `Testcontainers.PostgreSql` + `MassTransit` (test harness) + `WireMock.Net`

Integration tests verify the full HTTP pipeline of a single service against a real database. A `WebApplicationFactory<Program>` boots the service with a Testcontainers PostgreSQL instance substituted for the real database, and the MassTransit test harness substitutes for RabbitMQ. Downstream HTTP calls (e.g., ContactService → AccountService) are stubbed with WireMock.

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
GET  /health                                   → 200 Healthy
```
Factory note: `AuthServiceFactory` seeds the default admin via `DefaultAdmin` config settings so integration tests can obtain an Admin JWT to call the register endpoint.

**UserManagementService.IntegrationTests**
```
POST /api/users                           → 201, profile in DB
POST /api/users (duplicate userId)        → 400
GET  /api/users/{id}                      → 200 with all profile fields
GET  /api/users/{id} (missing)            → 404
GET  /api/users/{id}/role                 → 200 with role string
GET  /api/users/team                      → 200, list of {userId, displayName, role}
Consumer: publish UserRegistered          → profile row appears in DB
Consumer: publish UserRegistered twice    → idempotent, still one row
Username derived from email on creation
Username collision → numeric suffix appended
GET  /health                              → 200 Healthy
```

**AccountService.IntegrationTests**
```
POST /api/accounts                     → 201, AccountCreated published
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
POST /api/activities                           → 201, ActivityLogged published
POST /api/activities (empty subject)           → 400
GET  /api/activities                           → 200, array
GET  /api/activities/{id}                      → 200 with all fields
GET  /api/activities/{id} (missing)            → 404
GET  /api/activities?type=Task                 → filtered list
PUT  /api/activities/{id}                      → 200, fields updated
PUT  /api/activities/{id} (Task + completedAt) → 200, TaskCompleted published
DELETE /api/activities/{id}                    → 204
DELETE /api/activities/{id} (missing)          → 404
GET  /health                                   → 200 Healthy
```

**ReportingService.IntegrationTests**
```
GET  /api/reports/pipeline     → 200
GET  /api/reports/activities   → 200
GET  /api/reports/contacts     → 200
GET  /api/reports/dashboard    → 200
Consumer: DealCreated          → pipeline projection updated
Consumer: DealStageChanged     → projection moves deal between stages
Consumer: DealClosed           → no side effects (read-only snapshot)
Consumer: ActivityLogged       → activity rep projection updated
Consumer: ContactStatusChanged → contact funnel projection updated
GET  /health                   → 200 Healthy
```

### Test isolation

- Each test class receives its own `PostgreSqlContainer` via `IClassFixture<ServiceFactory>`.
- Seed only what each test needs; rely on the per-class container being a fresh database.
- The MassTransit test harness publishes to an in-memory bus; assert published messages with `ITestHarness.Published.Select<EventType>()`.

---

## Layer 3 — End-to-End Tests

**Project:** `EndToEnd.Tests/`
**Packages:** `Polly`

A `TestTokenController` in AuthService exposes invite and password-reset tokens so E2E tests can complete full round-trips without a real SMTP server. It is guarded by the `EnableTestEndpoints=true` configuration key, which is set in `docker-compose.yml` for the auth-service and absent in any production configuration.

E2E tests run against the full Docker Compose stack via the YARP gateway on `http://localhost:5000`. No `WebApplicationFactory` — these are plain HTTP calls. All services, databases, and RabbitMQ are real.

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
    UserManagementFlowTests.cs
    AccountContactFlowTests.cs
    DealFlowTests.cs
    ActivityFlowTests.cs
    ReportingFlowTests.cs
    GatewayAuthTests.cs
```

### RetryHelper

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
  → POST /auth/api/registration/register (admin JWT) → 200
  → Poll GET /users/api/users/{userId} until 200 (async consumer lag)
  → Profile exists, role is "Member"

Login with email
  → POST /auth/api/login/login → 200, token returned
  → GET /auth/api/login/me → claims match registration

Duplicate registration → second returns 409

Full invite round-trip
  → POST /auth/api/users/invite (admin JWT) → 200
  → GET /auth/api/test/tokens/invite?email={email} → token
  → POST /auth/api/registration/accept-invite { token, password } → 200
  → POST /auth/api/login/login with new password → 200

Full password-reset round-trip
  → POST /auth/api/auth/forgot-password → 200
  → GET /auth/api/test/tokens/password-reset?email={email} → token
  → POST /auth/api/auth/reset-password { token, newPassword } → 200
  → Login with new password → 200; login with old password → 401
```

**UserManagementFlowTests**
```
GET /users/api/users/team (admin)               → 200, array with userId/displayName/role
GET /users/api/users/{id}/role                  → 200, userId + role + isActive
Register new user → poll UMS → appears in team  (async consumer flow)
GET /admin/api/admin/users                      → 200, array with email/role/isActive
Deactivate user → login blocked (403) → reactivate → login succeeds (200)
Resend invite (self-registered user)            → 400 (no InviteToken)
Resend invite (invited user, stub profile)      → 200, returns userId + inviteSentAt
GET /users/api/users/audit → array with action/actorUserId/targetUserId/timestamp
```

**AccountContactFlowTests**
```
Full lifecycle
  → POST /accounts/api/accounts → 201, capture accountId
  → POST /contacts/api/contacts (with accountId) → 201, capture contactId
  → PUT /contacts/api/contacts/{contactId} { status: "Prospect" } → 200
  → GET /contacts/api/contacts/{contactId} → status is "Prospect"
  → DELETE /accounts/api/accounts/{accountId} → 204

Invalid account reference
  → POST /contacts/api/contacts with random accountId → 400
```

**DealFlowTests**
```
Pipeline lifecycle
  → Create account + contact
  → POST /deals/api/deals → 201, capture dealId
  → POST /deals/api/deals/{dealId}/contacts → 201
  → PUT /deals/api/deals/{dealId} { stage: "ClosedWon" } → 200
  → GET /pipeline/api/pipeline → ClosedWon column includes deal
  → GET /reports/api/reports/pipeline → pipeline projection updated
```

**ActivityFlowTests**
```
Log and complete a task
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

Auth-gated vs public routes
  → POST /auth/api/registration/register (no token) → 401 (Admin-only)
  → POST /auth/api/login/login                      → 200 or 400 (not 401, public)

Gateway health
  → GET /health → 200, all downstream services Healthy
```

### Running E2E tests

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

The `scripts/run-e2e.sh` script handles the full lifecycle. All required environment variables have safe defaults for local runs.

### E2E coverage

51 unique API endpoints across 7 services + gateway (includes 2 test-only endpoints). 51 covered (100%).

| Service | Endpoints | Covered | % |
|---------|-----------|---------|---|
| AuthService | 12 | 12 | 100% |
| UserManagementService | 11 | 11 | 100% |
| AccountService | 5 | 5 | 100% |
| ContactService | 5 | 5 | 100% |
| DealService | 8 | 8 | 100% |
| ActivityService | 5 | 5 | 100% |
| ReportingService | 4 | 4 | 100% |
| Gateway | 1 | 1 | 100% |
| **Total** | **51** | **51** | **100%** |

### Async event flows covered

| Event | Test |
|-------|------|
| UserRegistered → UserManagementService creates profile | AuthFlowTests, UserManagementFlowTests |
| ContactDeleted → DealService removes deal-contact associations | AccountContactFlowTests |
| ContactStatusChanged → ReportingService updates funnel | ReportingFlowTests |
| DealCreated → ReportingService updates pipeline | ReportingFlowTests |
| DealStageChanged → ReportingService updates pipeline counts | DealFlowTests |
| ActivityLogged → ReportingService updates activity projection | ActivityFlowTests |

---

## Package Reference

| Package | Layer | Purpose |
|---------|-------|---------|
| `xUnit` | Unit, Integration | Test framework |
| `Moq` | Unit | Interface mocking |
| `FluentAssertions` | Unit, Integration | Readable assertions |
| `Microsoft.EntityFrameworkCore.InMemory` | Unit | In-memory DB for repository tests |
| `RichardSzalay.MockHttp` | Unit | Mock `HttpMessageHandler` for typed HTTP clients |
| `Microsoft.AspNetCore.Mvc.Testing` | Integration | `WebApplicationFactory` — real ASP.NET Core pipeline in-process |
| `Testcontainers.PostgreSql` | Integration | Real PostgreSQL container per test class |
| `MassTransit` | Integration | In-memory bus harness for event assertions |
| `WireMock.Net` | Integration | Stub downstream HTTP services |
| `Polly` | E2E | Retry/polling for async event assertions |

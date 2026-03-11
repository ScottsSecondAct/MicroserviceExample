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

Four test projects exist covering the service layer only.

| Component | Unit | Integration | E2E |
|-----------|------|-------------|-----|
| AuthService — services | ✅ | ❌ | ❌ |
| AuthService — controllers | ✅ RegistrationController, LoginController | ❌ | ❌ |
| AuthService — repository | ❌ | ❌ | ❌ |
| AuthService — UserRoleClient | ❌ | ❌ | ❌ |
| UserManagementService — services | ✅ | ❌ | ❌ |
| UserManagementService — consumer | ✅ | ❌ | ❌ |
| UserManagementService — controller | ❌ | ❌ | ❌ |
| UserManagementService — repository | ❌ | ❌ | ❌ |
| ContactService — services | ✅ | ❌ | ❌ |
| ContactService — controller | ❌ | ❌ | ❌ |
| ContactService — repository | ❌ | ❌ | ❌ |
| ContactService — AccountClient | ❌ | ❌ | ❌ |
| AccountService — services | ✅ | ❌ | ❌ |
| AccountService — controller | ❌ | ❌ | ❌ |
| AccountService — repository | ❌ | ❌ | ❌ |

**Approximate coverage: ~43 tests across 11 files. Services only.**

---

## Layer 1 — Unit Tests

**Stack:** xUnit + Moq + FluentAssertions (all already in use)
**New package:** `RichardSzalay.MockHttp` for HTTP client tests

Unit tests verify a single class in isolation. All dependencies are mocked. No database, no network, no message broker.

### Missing controller tests

Three controllers have no tests. The pattern is already established in `LoginControllerTests` and `RegistrationControllerTests`: mock the service interface, assert the correct `IActionResult` type and status code.

```
AccountService.Tests/Controllers/
  AccountsControllerTests.cs     — GetAll, GetById, Create, Update, Delete
                                   success paths + 404 + validation failure

ContactService.Tests/Controllers/
  ContactsControllerTests.cs     — GetAll (with query params), GetById, Create, Update, Delete

UserManagementService.Tests/Controllers/
  UsersControllerTests.cs        — GetById, GetRole, GetTeam, Create
```

### Missing repository tests

All four repositories are untested. EF Core InMemory is already referenced in every test project — it just hasn't been used. Create a fresh in-memory database per test using a GUID database name to ensure full isolation.

```csharp
var options = new DbContextOptionsBuilder<ContactDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString())
    .Options;
using var ctx = new ContactDbContext(options);
var repo = new ContactRepository(ctx);
```

```
AuthService.Tests/Repository/
  UserRepositoryTests.cs              — FindByEmailAsync (found / not found), AddAsync

UserManagementService.Tests/Repository/
  UserProfileRepositoryTests.cs       — GetByUserIdAsync, AddAsync, GetAllAsync

ContactService.Tests/Repository/
  ContactRepositoryTests.cs           — GetAllAsync with no filter, status filter,
                                        ownerId filter, accountId filter;
                                        GetByIdAsync; Add + SaveChanges; Delete

AccountService.Tests/Repository/
  AccountRepositoryTests.cs           — GetAllAsync, GetByIdAsync, Add, Delete
```

### Missing HTTP client tests

`UserRoleClient` and `AccountClient` are typed `HttpClient` wrappers with failure-handling logic (default-to-`Unassigned`, fail-open). This logic is worth testing directly using `RichardSzalay.MockHttp` to mock the `HttpMessageHandler`.

```
AuthService.Tests/Services/
  UserRoleClientTests.cs
    — 200 with valid role string → returns correct UserRole enum value
    — 404 response → returns UserRole.Unassigned (default)
    — Network exception → returns UserRole.Unassigned (default)

ContactService.Tests/Services/
  AccountClientTests.cs
    — 200 response → returns true (account exists)
    — 404 response → returns false (account not found)
    — Network exception → returns true (fail-open behavior preserved)
```

---

## Layer 2 — Integration Tests

**New packages:**

| Package | Purpose |
|---------|---------|
| `Microsoft.AspNetCore.Mvc.Testing` | `WebApplicationFactory<Program>` — boots the real ASP.NET Core pipeline in-process |
| `Testcontainers.PostgreSql` | Spins up a real PostgreSQL container per test class |
| `MassTransit.Testing` | In-memory bus harness — assert events published without needing RabbitMQ |
| `WireMock.Net` | Mocks downstream HTTP services (e.g., mock AccountService when testing ContactService) |

Integration tests verify that the full HTTP pipeline of a single service works correctly against a real database. A `WebApplicationFactory<Program>` boots the service with a Testcontainers PostgreSQL instance substituted for the real database, and `MassTransit.Testing` substitutes for RabbitMQ. Downstream HTTP calls (e.g., ContactService → AccountService) are stubbed with WireMock.

### New projects

```
AuthService/src/AuthService.IntegrationTests/
UserManagementService/src/UserManagementService.IntegrationTests/
ContactService/src/ContactService.IntegrationTests/
AccountService/src/AccountService.IntegrationTests/
```

### Factory pattern

Each project has one factory class used as an `IClassFixture`. The factory:
1. Starts a PostgreSQL Testcontainer
2. Overrides the DbContext connection string
3. Replaces MassTransit with the in-memory test harness

```csharp
public class AccountServiceFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync() => await _db.StartAsync();
    public new async Task DisposeAsync() => await _db.DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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

E2E infrastructure should be deferred until after v1.3 (Deals & Pipeline). The richest E2E scenario is the full sales flow: *register → create account → create contact → create deal → transition stage*. The investment in Docker Compose test orchestration pays off more when the scenarios are complete.

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

Owner assignment
  → Register two users
  → Create contact, assign ownerId = second user's userId
  → GET /contacts/api/contacts?ownerId={secondUserId} → contact appears in list
```

**GatewayAuthTests**
```
Unauthenticated access to protected routes
  → GET /contacts/api/contacts (no token)  → 401
  → GET /accounts/api/accounts (no token)  → 401
  → POST /contacts/api/contacts (no token) → 401

Public routes accessible without token
  → POST /auth/api/registration/register   → 200 (not 401)
  → POST /auth/api/login/login             → 200 or 400 (not 401)

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

### Do before v1.3 (unit gap-fill, ~1–2 days)

The safety net that catches regressions when DealService reshapes the domain.

1. Controller tests — AccountsController, ContactsController, UsersController
2. Repository tests — all four repositories using EF Core InMemory
3. HTTP client tests — UserRoleClient, AccountClient using MockHttp

### Do alongside v1.3 (integration tests, per service as built)

Establish the `WebApplicationFactory` + Testcontainers + MassTransit harness pattern once for DealService, then backfill AccountService and ContactService at the same time. Writing integration tests while a service is fresh is significantly faster than retrofitting them later.

1. Set up `AccountService.IntegrationTests` and `ContactService.IntegrationTests` using the factory pattern above
2. As DealService is built, create `DealService.IntegrationTests` alongside it
3. Backfill `AuthService.IntegrationTests` and `UserManagementService.IntegrationTests` when convenient

### Defer until after v1.3 (E2E infrastructure)

The `EndToEnd.Tests` project and Docker Compose test orchestration. The scenarios are richer and the investment pays off more once Deals are in the picture.

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

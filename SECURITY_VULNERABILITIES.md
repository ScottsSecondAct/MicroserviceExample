# Security Vulnerabilities

This document identifies every security gap between the current codebase and production readiness. Issues are grouped by severity. Each entry names the affected files, explains the risk, and specifies the fix.

This is not a theoretical checklist — every issue was identified by reading the actual code.

---

## Critical

### 1. Downstream services are fully unauthenticated

**Files:** `UserManagementService/Program.cs`, `AccountService/Program.cs`, `ContactService/Program.cs`, `DealService/Program.cs`, `ActivityService/Program.cs`

**Risk:** The API gateway validates JWTs before forwarding requests, but the downstream services themselves perform no authentication. Any process that can reach them on the Docker network — or that bypasses the gateway entirely — has unrestricted read/write access to all data. There is no defence in depth. A single misconfigured network rule, a container escape, or a compromised sidecar would give an attacker full access to all four databases.

**Fix:** Each downstream service should validate the incoming JWT independently. The gateway should forward the `Authorization` header as-is (YARP does this by default). Each service registers the same JWT Bearer middleware as the gateway and marks all controllers `[Authorize]`. The shared secret and issuer/audience settings are already available via environment variables.

---

### 2. PBKDF2 iteration count is critically low

**File:** `AuthService/src/AuthService/Services/PasswordService.cs:14`

```csharp
iterationCount: 10000,  // NIST 2023 minimum: 600,000 for PBKDF2-SHA256
```

**Risk:** 10,000 PBKDF2-SHA256 iterations can be computed at roughly 1–2 million hashes per second on a consumer GPU. A leaked `auth` database table would allow an attacker to crack most passwords in minutes to hours. NIST SP 800-132 (2023 revision) recommends a minimum of 600,000 iterations for PBKDF2-SHA256.

**Fix:** Raise `iterationCount` to at least `600_000`. Also migrate to `ASP.NET Core Identity`'s `PasswordHasher<T>` or `Argon2id` via the `Konscious.Security.Cryptography` package, both of which are memory-hard and significantly more resistant to GPU cracking. Existing hashes require a migration strategy: re-hash on next successful login and store a version field in the hash string.

---

### 3. Timing attack in password comparison

**File:** `AuthService/src/AuthService/Services/PasswordService.cs:33-36`

```csharp
for (int i = 0; i < 32; i++)
{
    if (hashBytes[i + 16] != hash[i])
        return false;  // early exit leaks timing information
}
```

**Risk:** The early-return loop leaks timing information: a password that matches the first 31 bytes but not the 32nd takes longer to reject than one that fails at byte 0. An attacker who can make many login attempts and measure response times can use this to brute-force passwords byte by byte.

**Fix:** Replace the loop with `CryptographicOperations.FixedTimeEquals()` (available in `System.Security.Cryptography` since .NET Core 2.1):

```csharp
return CryptographicOperations.FixedTimeEquals(
    hashBytes.AsSpan(16, 32),
    hash.AsSpan(0, 32));
```

---

### ~~4. Hardcoded database passwords in docker-compose.yml~~ ✅ Fixed (v2.0)

**Fixed in:** v2.0 secrets management (Phase 1)

All `POSTGRES_PASSWORD` values in `docker-compose.yml` now use environment variable references (`${AUTH_DB_PASSWORD}`, `${USER_DB_PASSWORD}`, etc.). The `.env.example` file documents every required variable with guidance to generate strong random values. The `.env` file remains git-ignored. Phase 2 (Docker Secrets / Vault / cloud secret store) is still open.

---

### 5. RabbitMQ default credentials used everywhere (partially mitigated)

**Files:** `docker-compose.yml`, all six service `Program.cs` files

```csharp
h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");  // hardcoded fallback remains
```

**Status (v2.0):** `docker-compose.yml` now injects RabbitMQ credentials via `${RABBITMQ_USERNAME}` / `${RABBITMQ_PASSWORD}` environment variables rather than hardcoding `guest/guest`. The `.env.example` documents these variables.

**Remaining gap:** The `?? "guest"` fallback in every service `Program.cs` is still present. If the environment variable is missing or mis-spelled the service silently authenticates with default credentials rather than failing fast. The fallback strings should be removed so the application throws a clear startup error when credentials are absent.

**Fix:** Remove all `?? "guest"` fallbacks from `Program.cs` files. Create a dedicated RabbitMQ user with minimal permissions rather than relying on the default `guest` vhost.

---

### 6. RabbitMQ management UI exposed on the host

**File:** `docker-compose.yml:66`

```yaml
ports:
  - "15672:15672"
```

**Risk:** The RabbitMQ management UI (and its HTTP API) is accessible from the host machine and any network it is reachable from. Combined with the `guest/guest` default credentials, this gives an attacker full control over the message broker: they can read message bodies (which include `UserRegistered` events containing email addresses), inject messages, delete queues, and create new bindings.

**Fix:** Remove the port mapping entirely. The management UI is only needed for debugging and should never be reachable from outside the Docker network in production. If operator access is required, use an SSH tunnel or a VPN.

---

## High

### 7. Swagger/OpenAPI enabled unconditionally in production

**Files:** `AuthService/Program.cs`, `AccountService/Program.cs`, `ContactService/Program.cs`, `DealService/Program.cs`, `ActivityService/Program.cs` (all unconditional)

```csharp
app.UseSwagger();       // no environment check
app.UseSwaggerUI();
```

**Risk:** Swagger exposes the complete API surface — all endpoints, request/response schemas, and authentication requirements — to anyone who can reach the service. In production this is a reconnaissance gift to an attacker. `UserManagementService` correctly gates Swagger behind `app.Environment.IsDevelopment()`; the others do not.

**Fix:** Wrap all Swagger middleware in an environment check in every service:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

---

### ~~8. No rate limiting on authentication endpoints~~ ✅ Fixed (v2.0)

**Fixed in:** v2.0 hardening

Rate limiting is implemented at the YARP gateway using ASP.NET Core's built-in `AddRateLimiter`. Per-IP and per-user limits are applied across all routes; thresholds are configurable via environment variables.

---

### ~~9. No refresh token mechanism — long-lived sessions or forced logouts~~ ✅ Fixed (v2.0)

**Fixed in:** v2.0 refresh token rotation

Refresh token rotation is now implemented in AuthService. Login returns both a JWT and an opaque `refreshToken`. The `POST /api/login/refresh` endpoint accepts a valid refresh token, issues a new JWT, and stores a rotated replacement refresh token — invalidating the one that was consumed. Refresh tokens are stored in the `AuthService` database (`RefreshToken` table via `IRefreshTokenRepository`) and carry their own expiry. This enables forced logout by deleting stored tokens.

**Remaining gap:** The JWT expiry is still 2 hours rather than the shorter window (15 minutes) originally recommended. While refresh rotation now exists, a stolen JWT remains valid for up to 2 hours without a revocation mechanism at the gateway level.

---

### 10. Docker containers run as root

**Files:** All six `Dockerfile` files

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
# No USER directive — runs as root
ENTRYPOINT ["dotnet", "AuthService.dll"]
```

**Risk:** If an attacker achieves remote code execution within a container, they run as root inside the container. This significantly increases the blast radius: root inside a container can often escape via kernel vulnerabilities, access mounted volumes, or pivot to the host in misconfigured environments.

**Fix:** Add a non-root user to all Dockerfiles:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
RUN adduser --disabled-password --gecos "" appuser
COPY --from=publish /app/publish .
USER appuser
ENTRYPOINT ["dotnet", "AuthService.dll"]
```

---

### 11. All inter-service communication is unencrypted HTTP

**Files:** `docker-compose.yml` (all service addresses), all six `appsettings.json` files

```yaml
ASPNETCORE_URLS: http://+:8080
```

**Risk:** Service-to-service traffic — including JWT tokens forwarded by the gateway, `UserRegistered` event payloads containing email addresses, and all CRM data — travels in plaintext over the Docker bridge network. While Docker networks provide some isolation, this is not sufficient: a compromised container on the same network can intercept all traffic passively.

**Fix:** For production, enable TLS between services using internal certificates issued by a private CA (e.g., via HashiCorp Vault PKI or cert-manager in Kubernetes). An intermediate step is to run all services behind a service mesh (Envoy, Linkerd) which handles mTLS transparently.

---

## Medium

### ~~12. Password complexity is not enforced~~ ✅ Fixed (v2.1)

**Fixed in:** v2.1 Enterprise User Management

Server-side password policy is enforced in `AuthService` before hashing. Policy requires minimum length, uppercase, lowercase, digit, and special character. Rules are configurable via `appsettings.json`. The frontend includes a strength indicator that reflects the same policy.

---

### 13. No security response headers

**Files:** All `Program.cs` files, `ApiGateway/Program.cs`

**Risk:** The application returns no security-relevant HTTP response headers. Without these, browsers allow a range of attacks:
- No `X-Content-Type-Options: nosniff` → MIME-type sniffing attacks
- No `X-Frame-Options: DENY` → clickjacking
- No `Strict-Transport-Security` → SSL stripping
- No `Content-Security-Policy` → cross-site scripting escalation
- No `Referrer-Policy` → token/URL leakage in the `Referer` header

**Fix:** Add the `NWebsec.AspNetCore.Middleware` package or configure headers manually in the gateway's middleware pipeline. At minimum:

```csharp
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    ctx.Response.Headers.Append("X-Frame-Options", "DENY");
    ctx.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});
```

---

### 14. Overly broad CORS policy

**File:** `ApiGateway/src/ApiGateway/Program.cs:40-42`

```csharp
policy.WithOrigins(allowedOrigins)
      .AllowAnyHeader()
      .AllowAnyMethod()
```

**Risk:** Permitting any header and any HTTP method is broader than needed. While origins are correctly restricted, allowing `DELETE`, `PUT`, and arbitrary headers from the browser surface is unnecessary for a read-heavy frontend.

**Fix:** Scope to the specific methods and headers the frontend actually sends:

```csharp
.WithMethods("GET", "POST", "PUT", "DELETE")
.WithHeaders("Authorization", "Content-Type")
```

---

### 15. Health check endpoint leaks infrastructure topology

**Files:** `ApiGateway/src/ApiGateway/Program.cs:46-58`, all service `Program.cs` files

**Risk:** `GET /health` is publicly accessible and returns the health status of every downstream service by name. This exposes the internal service topology (service names, dependency graph) to unauthenticated callers — useful reconnaissance for an attacker.

**Fix:** Either require authentication for the detailed health endpoint, or return only a simple `Healthy`/`Unhealthy` boolean to unauthenticated callers using the `ResponseWriter` option:

```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = (ctx, report) =>
    {
        ctx.Response.ContentType = "text/plain";
        return ctx.Response.WriteAsync(report.Status.ToString());
    }
}).RequireAuthorization();  // or a specific policy
```

Expose the detailed report only to internal monitoring systems.

---

### 16. Debug logging enabled in production

**Files:** All six service `Program.cs` files

```csharp
builder.Logging.AddDebug();
```

**Risk:** `AddDebug()` writes to the system debugger and can capture request details, connection strings (if EF Core debug logging is enabled), and internal state. At `Information` log level, ASP.NET Core logs incoming requests including URL paths which may contain IDs. A misconfigured log shipper can send this data to insecure destinations.

**Fix:** Remove `AddDebug()` from all services. In production, use structured logging (Serilog or Microsoft.Extensions.Logging with a JSON formatter) shipping to a centralised log aggregator (e.g., Seq, Elasticsearch, CloudWatch). Ensure connection strings and JWT secrets are never logged — set EF Core log level to `Warning` or higher in production.

---

### 17. JWT configuration uses placeholder values

**File:** `docker-compose.yml:82-83`, `ApiGateway/src/ApiGateway/appsettings.json:12-13`

```yaml
JwtSettings__Issuer: "https://localhost"
JwtSettings__Audience: "YourAppUsers"
```

**Risk:** `Issuer` is `"https://localhost"` and `Audience` is `"YourAppUsers"` — both are placeholders. In production, the issuer should be the canonical URL of the AuthService (e.g., `https://auth.yourdomain.com`). While validation is enabled and technically enforces these placeholder values, they provide no real binding to a specific deployment — any token issued by any deployment with these same placeholder values will be accepted by any other.

**Fix:** Set issuer and audience to deployment-specific values via environment variables, distinct per environment (staging vs. production). Document the required values in `.env.example`.

---

### 18. AllowedHosts permits any host header

**Files:** `AuthService/src/AuthService/appsettings.json:4`, similar in other services

```json
"AllowedHosts": "*"
```

**Risk:** `AllowedHosts: "*"` disables host header validation entirely. This leaves the application open to host header injection attacks, which can be used to poison password reset links, cache poisoning, and SSRF in certain configurations.

**Fix:** Set `AllowedHosts` to the specific hostnames the service will receive requests on:

```json
"AllowedHosts": "api.yourdomain.com;auth.yourdomain.com"
```

---

### 19. No request body size limits

**Files:** All six service `Program.cs` files

**Risk:** ASP.NET Core's default request body size limit is 30MB. A malicious client can send large JSON payloads to any endpoint, causing excessive memory allocation and potential out-of-memory conditions — a denial of service attack requiring no authentication for the public registration and login endpoints.

**Fix:** Set a tight limit appropriate for the API:

```csharp
builder.Services.Configure<KestrelServerOptions>(options =>
    options.Limits.MaxRequestBodySize = 65_536); // 64KB
```

Or apply per-endpoint using `[RequestSizeLimit(65536)]`.

---

## Low / Informational

### 20. OpenTelemetry exports traces to console

**Files:** All six service `Program.cs` files

```csharp
.AddConsoleExporter()
```

**Risk:** Console trace export is for development only. In production it floods stdout with trace data, degrading performance and potentially exposing sensitive request/response data (URLs containing IDs, span attributes) in log aggregators that capture stdout.

**Fix:** Replace `AddConsoleExporter()` with an OTLP exporter targeting a collector (Jaeger, Zipkin, Grafana Tempo, AWS X-Ray):

```csharp
.AddOtlpExporter(o => o.Endpoint = new Uri(configuration["Otlp:Endpoint"]!))
```

---

### ~~21. No audit trail on CRM entities~~ ✅ Fixed (v2.0)

**Fixed in:** v2.0 hardening

`IsDeleted`/`DeletedAt` soft-delete fields are present on all CRM entities. A lightweight audit log (actor, action, timestamp) is maintained per service. Hard deletes have been replaced with soft deletes across all CRUD endpoints. Identity-level audit events (invite sent, role change, deactivation) are stored in UserManagementService and accessible via `GET /api/users/audit`.

---

### 22. EnsureCreated() is not safe for production schema management

**Files:** All six service `Program.cs` files

```csharp
db.Database.EnsureCreated();
```

**Risk:** `EnsureCreated()` creates the schema if it doesn't exist but never applies changes to an existing schema. A new column, index, or constraint will silently be missing in any environment where the database was previously created. This can cause runtime failures that are difficult to diagnose and cannot be tracked or rolled back.

**Fix:** Replace with EF Core migrations (`dotnet ef migrations add`, `dotnet ef database update`) run as a startup step or a separate migration job. Migrations provide a versioned, auditable history of schema changes and support rollbacks.

---

### 23. No input length limits on entity string fields

**Files:** All entity model classes (`User.cs`, `UserProfile.cs`, `Contact.cs`, `Account.cs`, `Deal.cs`, `Activity.cs`)

**Risk:** String fields like `Email`, `FirstName`, `LastName`, `Name`, `Website` have no `[MaxLength]` attributes. Without these, EF Core generates `text` columns in PostgreSQL with no length constraint. A client can submit strings of arbitrary length, consuming excessive storage and potentially triggering log injection if the strings end up in log messages.

**Fix:** Add `[MaxLength]` data annotations appropriate to each field:

```csharp
[MaxLength(256)] public string Email { get; set; } = string.Empty;
[MaxLength(100)] public string FirstName { get; set; } = string.Empty;
[MaxLength(2048)] public string? Website { get; set; }
```

---

## Summary Table

| # | Issue | Severity | Effort | Status |
|---|-------|----------|--------|--------|
| 1 | Downstream services unauthenticated | Critical | Medium | Open |
| 2 | PBKDF2 iteration count too low | Critical | Low | Open |
| 3 | Timing attack in password comparison | Critical | Low | Open |
| 4 | ~~Hardcoded database passwords~~ | ~~Critical~~ | ~~Low~~ | **Fixed v2.0** |
| 5 | RabbitMQ default credentials | Critical | Low | Partial (docker-compose fixed; `?? "guest"` fallback remains) |
| 6 | RabbitMQ management UI exposed | Critical | Low | Open |
| 7 | Swagger enabled in production | High | Low | Open |
| 8 | ~~No rate limiting on auth endpoints~~ | ~~High~~ | ~~Medium~~ | **Fixed v2.0** |
| 9 | ~~No refresh token / no revocation~~ | ~~High~~ | ~~High~~ | **Fixed v2.0** |
| 10 | Containers run as root | High | Low | Open |
| 11 | Inter-service traffic unencrypted | High | High | Open |
| 12 | ~~Password complexity not enforced~~ | ~~Medium~~ | ~~Low~~ | **Fixed v2.1** |
| 13 | No security response headers | Medium | Low | Open |
| 14 | CORS policy too broad | Medium | Low | Open |
| 15 | Health endpoint leaks topology | Medium | Low | Open |
| 16 | Debug logging in production | Medium | Low | Open |
| 17 | JWT issuer/audience are placeholders | Medium | Low | Open |
| 18 | AllowedHosts permits any host | Medium | Low | Open |
| 19 | No request body size limits | Medium | Low | Open |
| 20 | OpenTelemetry exporting to console | Low | Low | Open |
| 21 | ~~No audit trail on CRM entities~~ | ~~Low~~ | ~~High~~ | **Fixed v2.0** |
| 22 | EnsureCreated() in production | Low | Medium | Open |
| 23 | No input length limits | Low | Low | Open |

### Roadmap tracking

All open issues are now tracked in ROADMAP.md:

| Issues | Milestone |
|---|---|
| 1, 2, 3, 5, 6, 7, 10, 13, 14, 15, 16, 17, 18, 19, 23 | v2.3 — Security Hardening |
| 11 (mTLS), 20 (OTel exporter) | v3.0 — Infrastructure Hardening |
| 22 (EnsureCreated) | v3.0 — Prerequisites |

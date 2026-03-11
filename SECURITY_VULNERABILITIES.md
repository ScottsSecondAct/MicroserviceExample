# Security Vulnerabilities

This document identifies every security gap between the current codebase and production readiness. Issues are grouped by severity. Each entry names the affected files, explains the risk, and specifies the fix.

This is not a theoretical checklist — every issue was identified by reading the actual code.

---

## Critical

### 1. Downstream services are fully unauthenticated

**Files:** `UserManagementService/Program.cs`, `AccountService/Program.cs`, `ContactService/Program.cs`

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

### 4. Hardcoded database passwords in docker-compose.yml

**File:** `docker-compose.yml:7, 22, 37, 51, 80, 101, 118, 135`

```yaml
POSTGRES_PASSWORD: auth_pass   # and user_pass, account_pass, contact_pass
```

**Risk:** These passwords are committed to source control and are trivially guessable. Anyone with access to the repository has the database credentials. The connection strings in the service environment blocks repeat these values verbatim.

**Fix:** Replace every hardcoded credential with an environment variable reference and add them to `.env` (which is already git-ignored):

```yaml
POSTGRES_PASSWORD: ${AUTH_DB_PASSWORD}
```

Generate strong random passwords (e.g., `openssl rand -base64 32`) and document the required variables in `.env.example`. Consider Docker Secrets for a more hardened setup.

---

### 5. RabbitMQ default credentials used everywhere

**Files:** `docker-compose.yml:86-87`, all five `Program.cs` files

```yaml
RabbitMQ__Username: "guest"
RabbitMQ__Password: "guest"
```

```csharp
h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");  // hardcoded fallback
```

**Risk:** `guest/guest` are the RabbitMQ default credentials, widely known and targeted by automated scanners. The `?? "guest"` fallback in every `Program.cs` means the service silently authenticates with default credentials if the configuration key is missing — a dangerous silent failure.

**Fix:** Create a dedicated RabbitMQ user with a strong password and only the vhosts/permissions it needs. Store credentials as environment variables. Remove all `?? "guest"` fallbacks — the application should fail fast with a clear error if credentials are missing rather than silently using defaults.

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

**Files:** `AuthService/Program.cs:71-72`, `AccountService/Program.cs` (unconditional), `ContactService/Program.cs` (unconditional)

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

### 8. No rate limiting on authentication endpoints

**Files:** `AuthService/src/AuthService/Controllers/LoginController.cs`, `RegistrationController.cs`, `ApiGateway/src/ApiGateway/Program.cs`

**Risk:** `POST /auth/api/login/login` and `POST /auth/api/registration/register` have no rate limiting. An attacker can make unlimited login attempts, enabling:
- Credential stuffing (testing breached username/password pairs)
- Password brute force (especially dangerous given the low PBKDF2 iteration count)
- Account enumeration via timing differences between "user not found" and "wrong password" responses
- Registration spam

**Fix:** Add ASP.NET Core's built-in rate limiter (`AddRateLimiter`) at the gateway for the `/auth/**` route. A fixed-window policy of 10 requests per minute per IP is a reasonable starting point for auth endpoints. Also add a per-user sliding window for login to limit password guessing even from distributed sources.

---

### 9. No refresh token mechanism — long-lived sessions or forced logouts

**File:** `AuthService/src/AuthService/Services/JwtTokenService.cs:37`

```csharp
expires: DateTime.UtcNow.AddHours(2),
```

**Risk:** JWTs are stateless and cannot be revoked. A stolen token is valid for the full 2-hour window with no way to invalidate it. In practice, UX pressure drives developers to increase expiry times — which makes token theft more damaging. There is also no way to force logout all sessions when a user changes their password or when suspicious activity is detected.

**Fix:** Implement refresh token rotation: issue short-lived JWTs (15 minutes) alongside long-lived, opaque refresh tokens stored in the `AuthService` database. The refresh token endpoint issues a new JWT and rotates the refresh token. Implement refresh token revocation. This also enables "log out all devices" functionality.

---

### 10. Docker containers run as root

**Files:** All five `Dockerfile` files

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

**Files:** `docker-compose.yml` (all service addresses), all `appsettings.json` files

```yaml
ASPNETCORE_URLS: http://+:8080
```

**Risk:** Service-to-service traffic — including JWT tokens forwarded by the gateway, `UserRegistered` event payloads containing email addresses, and all CRM data — travels in plaintext over the Docker bridge network. While Docker networks provide some isolation, this is not sufficient: a compromised container on the same network can intercept all traffic passively.

**Fix:** For production, enable TLS between services using internal certificates issued by a private CA (e.g., via HashiCorp Vault PKI or cert-manager in Kubernetes). An intermediate step is to run all services behind a service mesh (Envoy, Linkerd) which handles mTLS transparently.

---

## Medium

### 12. Password complexity is not enforced

**File:** `AuthService/src/AuthService/Controllers/RegistrationController.cs`

**Risk:** There is no minimum password length check beyond what the frontend enforces (`minLength={6}` in the React form). Frontend validation is trivially bypassed by calling the API directly. A 6-character minimum with no complexity requirements allows extremely weak passwords that are trivially cracked once a database is leaked.

**Fix:** Add server-side password policy validation in `RegistrationService` before hashing: minimum 12 characters, at least one uppercase letter, one digit, and one special character. Optionally integrate with [HaveIBeenPwned's Pwned Passwords API](https://haveibeenpwned.com/API/v3#PwnedPasswords) to reject known-breached passwords.

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

**Files:** All five `Program.cs` files

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

**Files:** All service `Program.cs` files

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

**Files:** All five `Program.cs` files

```csharp
.AddConsoleExporter()
```

**Risk:** Console trace export is for development only. In production it floods stdout with trace data, degrading performance and potentially exposing sensitive request/response data (URLs containing IDs, span attributes) in log aggregators that capture stdout.

**Fix:** Replace `AddConsoleExporter()` with an OTLP exporter targeting a collector (Jaeger, Zipkin, Grafana Tempo, AWS X-Ray):

```csharp
.AddOtlpExporter(o => o.Endpoint = new Uri(configuration["Otlp:Endpoint"]!))
```

---

### 21. No audit trail on CRM entities

**Files:** All service entity models and repositories

**Risk:** Hard deletes with no audit log make it impossible to determine who created, modified, or deleted a record. This is a compliance requirement for most data regulations (GDPR, SOC 2, HIPAA) and essential for forensic investigation after a breach or data integrity incident.

**Fix:** Add `CreatedBy`, `UpdatedBy`, `DeletedBy` (Guid, populated from the JWT `UserId` claim), and soft-delete fields (`IsDeleted`, `DeletedAt`) to all CRM entities. Populate them automatically via an EF Core `SaveChanges` interceptor that reads the current user from `IHttpContextAccessor`. This is already noted as a v2.0 roadmap item.

---

### 22. EnsureCreated() is not safe for production schema management

**Files:** All five `Program.cs` files

```csharp
db.Database.EnsureCreated();
```

**Risk:** `EnsureCreated()` creates the schema if it doesn't exist but never applies changes to an existing schema. A new column, index, or constraint will silently be missing in any environment where the database was previously created. This can cause runtime failures that are difficult to diagnose and cannot be tracked or rolled back.

**Fix:** Replace with EF Core migrations (`dotnet ef migrations add`, `dotnet ef database update`) run as a startup step or a separate migration job. Migrations provide a versioned, auditable history of schema changes and support rollbacks.

---

### 23. No input length limits on entity string fields

**Files:** All entity model classes (`User.cs`, `UserProfile.cs`, `Contact.cs`, `Account.cs`)

**Risk:** String fields like `Email`, `FirstName`, `LastName`, `Name`, `Website` have no `[MaxLength]` attributes. Without these, EF Core generates `text` columns in PostgreSQL with no length constraint. A client can submit strings of arbitrary length, consuming excessive storage and potentially triggering log injection if the strings end up in log messages.

**Fix:** Add `[MaxLength]` data annotations appropriate to each field:

```csharp
[MaxLength(256)] public string Email { get; set; } = string.Empty;
[MaxLength(100)] public string FirstName { get; set; } = string.Empty;
[MaxLength(2048)] public string? Website { get; set; }
```

---

## Summary Table

| # | Issue | Severity | Effort |
|---|-------|----------|--------|
| 1 | Downstream services unauthenticated | Critical | Medium |
| 2 | PBKDF2 iteration count too low | Critical | Low |
| 3 | Timing attack in password comparison | Critical | Low |
| 4 | Hardcoded database passwords | Critical | Low |
| 5 | RabbitMQ default credentials | Critical | Low |
| 6 | RabbitMQ management UI exposed | Critical | Low |
| 7 | Swagger enabled in production | High | Low |
| 8 | No rate limiting on auth endpoints | High | Medium |
| 9 | No refresh token / no revocation | High | High |
| 10 | Containers run as root | High | Low |
| 11 | Inter-service traffic unencrypted | High | High |
| 12 | Password complexity not enforced | Medium | Low |
| 13 | No security response headers | Medium | Low |
| 14 | CORS policy too broad | Medium | Low |
| 15 | Health endpoint leaks topology | Medium | Low |
| 16 | Debug logging in production | Medium | Low |
| 17 | JWT issuer/audience are placeholders | Medium | Low |
| 18 | AllowedHosts permits any host | Medium | Low |
| 19 | No request body size limits | Medium | Low |
| 20 | OpenTelemetry exporting to console | Low | Low |
| 21 | No audit trail on CRM entities | Low | High |
| 22 | EnsureCreated() in production | Low | Medium |
| 23 | No input length limits | Low | Low |

### Recommended priority order

**Fix immediately (low effort, critical/high risk):**
Issues 2, 3, 4, 5, 6, 7, 10, 13, 14, 16, 17, 18, 19 — most are one-line or one-file changes.

**Fix before any real user data is stored:**
Issues 1, 8, 12, 15 — these directly affect data confidentiality and availability.

**Plan for v2.0:**
Issues 9, 11, 21, 22, 23 — these require architectural work and are tracked in the roadmap.

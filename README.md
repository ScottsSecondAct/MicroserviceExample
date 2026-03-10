# MicroserviceExample
[![Open Source](https://img.shields.io/badge/Open%20Source-Yes-green.svg)](https://github.com/ScottsSecondAct/MicroserviceExample) [![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT) ![AI Assisted](https://img.shields.io/badge/AI%20Assisted-Claude-blue?logo=anthropic) [![Release](https://github.com/ScottsSecondAct/MicroserviceExample/actions/workflows/release.yml/badge.svg)](https://github.com/ScottsSecondAct/MicroserviceExample/actions/workflows/release.yml)

A working two-service microservices system in **ASP.NET Core (.NET 9)** — authentication, user profile management, JWT-secured inter-service communication, and a React frontend. Built to demonstrate real microservice patterns rather than toy examples.

## Why This Project

Most microservice tutorials show a diagram with boxes and arrows, then implement a single monolith with a split folder structure. This project implements the real thing: two independently deployable services, each with its own database, communicating over HTTP with shared DTOs.

The interesting problems are in the seams. When a user registers, AuthService and UserManagementService must agree on who owns the user's role — and they can't share a database to coordinate. The JWT token must carry enough claims for AuthService to answer `GET /me` without a database round-trip, yet the canonical profile lives in UserManagementService. These are the actual design tensions in distributed systems, not toy problems.

This project was developed with AI assistance (Anthropic's Claude) as a design and implementation collaborator. Architecture decisions, service boundaries, and every tradeoff were made and understood by hand. The AI accelerated the work; it didn't replace the thinking.

## Architecture

```
  Client (React)
      │
      ├── POST /auth/api/registration/register ──► AuthService :5188
      │                                                │
      │                                                ├─ hash password
      │                                                ├─ save User (email, hash)
      │                                                └─ POST /api/users ──► UserManagementService :5151
      │                                                                            └─ create UserProfile
      │                                                                            └─ return { role: Member }
      │
      ├── POST /auth/api/login/login ─────────────► AuthService :5188
      │                                                └─ verify password → issue JWT
      │
      ├── GET  /auth/api/login/me ─────────────────► AuthService :5188
      │                                                └─ decode JWT claims → { userId, email, role }
      │
      └── GET  /users/api/users/{userId} ──────────► UserManagementService :5151
                                                       └─ return UserProfile
```

### Services

**AuthService** (HTTP :5188 / HTTPS :7043)
- Owns authentication: registration, login, and JWT issuance
- Calls UserManagementService synchronously on registration to create the user profile and receive the assigned role
- JWT tokens carry `UserId`, `Email`, and `Role` claims; expire after 2 hours

**UserManagementService** (HTTP :5151 / HTTPS :7158)
- Owns user profiles: `UserId`, `Email`, `Role`, `DisplayName`, `CreatedAt`
- Only entry point from outside: `GET /api/users/{userId}`
- `POST /api/users` is for AuthService's internal use only

**SharedLibrary**
- `CreateUserProfileRequest` / `CreateUserProfileResponse` DTOs
- `UserRole` enum: `Unassigned`, `Member`, `Admin`
- No external dependencies; referenced by both services

### Layered pattern (per service)

```
Controller → IService → IRepository → EF DbContext → PostgreSQL
```

Each layer is defined by an interface, enabling test doubles at any boundary.

## Build & Run

**Requirements:** .NET 9 SDK, PostgreSQL

```sh
# Clone and build
git clone https://github.com/ScottsSecondAct/MicroserviceExample
cd MicroserviceExample
dotnet build MicroserviceExample.sln

# Run tests
dotnet test MicroserviceExample.sln

# Run a single service
dotnet run --project AuthService/src/AuthService/
dotnet run --project UserManagementService/src/UserManagementService/

# Run a single test class
dotnet test --filter "FullyQualifiedName~LoginServiceTests" \
  AuthService/src/AuthService.Tests/AuthService.Tests.csproj
```

Configure `ConnectionStrings:UserManagementDbConnection` in `UserManagementService/src/UserManagementService/appsettings.json` and the equivalent in AuthService before running. The AuthService base URL for UserManagementService is set via `ServiceUrls:UserManagementService` (defaults to `https://usermanagementservice`).

### Frontend

```sh
cd frontend
npm install
npm run dev    # http://localhost:5173
```

Vite proxies `/auth/*` to `localhost:5188` and `/users/*` to `localhost:5151`, so no CORS configuration is needed for local development.

## API Reference

### AuthService

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `POST` | `/api/registration/register` | — | Register a new user |
| `POST` | `/api/login/login` | — | Login and receive a JWT |
| `GET`  | `/api/login/me` | Bearer | Current user from JWT claims |

**Register** `POST /api/registration/register`
```json
// Request
{ "email": "user@example.com", "password": "secret123" }

// Response 200
{ "message": "User registered successfully." }
```

**Login** `POST /api/login/login`
```json
// Request
{ "email": "user@example.com", "password": "secret123" }

// Response 200
{ "token": "<jwt>" }
```

**Me** `GET /api/login/me`
```json
// Response 200
{ "userId": "<guid>", "email": "user@example.com", "role": "Member" }
```

### UserManagementService

| Method | Path | Description |
|--------|------|-------------|
| `GET`  | `/api/users/{userId}` | Fetch user profile |
| `POST` | `/api/users` | Create profile (AuthService internal) |

## Testing

- **xUnit** for test structure, **Moq** for mocking interfaces, **FluentAssertions** for readable assertions
- **EF Core InMemory** provider for repository and DbContext tests without a real database
- Tests cover controllers, services, and repositories; test files mirror the source structure under `*.Tests/` projects

## Known Limitations & Potential Improvements

- **Async messaging** — registration is tightly coupled; if UserManagementService is down, registration fails. A message broker (RabbitMQ, Kafka) would decouple them.
- **API Gateway** — clients hit each service directly. A gateway (YARP, Ocelot) would centralize routing and auth validation.
- **Centralized secrets** — JWT key and connection strings are in `appsettings.json`. A secrets manager or environment variable injection would be more production-appropriate.
- **Health checks** — no `/health` endpoints. `AddHealthChecks()` is needed for container orchestration.
- **Distributed tracing** — OpenTelemetry would allow tracing a registration request across both services.
- **Docker / docker-compose** — no containerization; a `docker-compose.yml` with both services and PostgreSQL would make local development self-contained.
- **Role ownership** — AuthService caches `Role` on its own `User` entity, duplicating UserManagementService's source of truth.

## Development Process & AI Collaboration

This project was built with AI assistance (Claude) as a design partner and implementation accelerator:

- **Service boundaries**: Deciding what each service owns — especially who holds the user's role and how it flows across the registration sequence — was an explicit design discussion, not an ad-hoc implementation choice.
- **Shared library design**: The tradeoff between a shared library (tight compile-time coupling) and duplicated DTOs (loose coupling, more drift risk) was weighed deliberately for a learning project.
- **Test architecture**: The decision to test each layer independently via interfaces, and to use EF Core InMemory rather than mocking DbContext directly, came from reasoning about what each test should actually verify.

Every line was reviewed and understood before integration.

## Skills Demonstrated

- **ASP.NET Core**: Controller routing, dependency injection, middleware pipeline, JWT Bearer authentication, `HttpClientFactory` for typed clients
- **Microservice design**: Database-per-service, inter-service HTTP communication, shared DTO library, service boundary decisions
- **Entity Framework Core**: Code-first models, PostgreSQL with Npgsql, repository pattern over DbContext
- **Security**: Password hashing, JWT token generation and validation, claims-based identity, `[Authorize]` attribute enforcement
- **Testing**: xUnit, Moq, FluentAssertions, EF Core InMemory; controller, service, and repository test layers
- **React**: Hooks (`useState`, `useEffect`), token persistence, multi-service API client, Vite dev proxy

## License

MIT — Copyright (c) 2026 Scott Davis


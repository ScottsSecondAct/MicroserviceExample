# Contributing to MicroserviceExample

This project is open to contributions from **California State University, Sacramento** Computer Science and Computer Engineering students. Whether you are looking for a real-world open-source experience, a senior project component, or just want to build something with professional-grade tooling — you are in the right place.

---

## Table of Contents

1. [Before You Start](#1-before-you-start)
2. [Development Environment Setup](#2-development-environment-setup)
   - 2.1 [Prerequisites](#21-prerequisites)
   - 2.2 [Fork and Clone](#22-fork-and-clone)
   - 2.3 [Environment Configuration](#23-environment-configuration)
   - 2.4 [Running the Full Stack with Docker Compose](#24-running-the-full-stack-with-docker-compose)
   - 2.5 [Running Services Locally (without Docker)](#25-running-services-locally-without-docker)
   - 2.6 [Running the Frontend](#26-running-the-frontend)
   - 2.7 [Verifying Your Setup](#27-verifying-your-setup)
3. [Finding Something to Work On](#3-finding-something-to-work-on)
4. [Workflow](#4-workflow)
   - 4.1 [Branching](#41-branching)
   - 4.2 [Making Changes](#42-making-changes)
   - 4.3 [Running Tests](#43-running-tests)
   - 4.4 [Opening a Pull Request](#44-opening-a-pull-request)
   - 4.5 [Code Review](#45-code-review)
5. [Coding Standards](#5-coding-standards)
6. [Testing Requirements](#6-testing-requirements)
7. [Project Documentation to Read First](#7-project-documentation-to-read-first)
8. [Getting Help](#8-getting-help)
9. [Code of Conduct](#9-code-of-conduct)

---

## 1. Before You Start

Read these three documents before writing a single line of code. They are short and will save you hours:

| Document | What it covers |
|---|---|
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | How the eight services fit together, which databases they own, how they communicate |
| [`ROADMAP.md`](ROADMAP.md) | What has been built and what is planned — every open GitHub issue maps to an item here |
| [`TESTING.md`](TESTING.md) | The three-layer test strategy; required reading before writing any tests |

The tech stack at a glance:

| Layer | Technology |
|---|---|
| Backend services | ASP.NET Core (.NET 9), C# |
| API gateway | YARP reverse proxy |
| Async messaging | RabbitMQ + MassTransit |
| Database | PostgreSQL 16 (one per service), Entity Framework Core |
| Frontend | React 18 + Vite, TanStack Query, Tailwind CSS, shadcn/ui |
| Containerization | Docker + Docker Compose |
| Structured logging | Serilog → Seq |
| Testing | xUnit, Moq, FluentAssertions, Testcontainers, WireMock.Net |

---

## 2. Development Environment Setup

### 2.1 Prerequisites

Install the following before cloning the repo. All are free.

| Tool | Minimum version | Download |
|---|---|---|
| Git | 2.40 | https://git-scm.com |
| .NET SDK | 9.0 | https://dotnet.microsoft.com/download |
| Docker Desktop | 24.0 (includes Compose v2) | https://www.docker.com/products/docker-desktop |
| Node.js | 20 LTS | https://nodejs.org |
| A code editor | — | [VS Code](https://code.visualstudio.com) recommended |

**Windows users:** Use WSL 2 (Ubuntu 22.04 or 24.04). Docker Desktop integrates with WSL 2 automatically. All shell commands in this guide assume a bash-compatible shell.

**Verify your installs:**
```bash
dotnet --version    # should print 9.x.x
docker compose version  # should print v2.x.x
node --version      # should print v20.x.x
```

### 2.2 Fork and Clone

1. Click **Fork** on the [GitHub repository page](https://github.com/ScottsSecondAct/MicroserviceExample).
2. Clone your fork:

```bash
git clone git@github.com:<your-username>/MicroserviceExample.git
cd MicroserviceExample
```

3. Add the upstream remote so you can pull in future changes:

```bash
git remote add upstream https://github.com/ScottsSecondAct/MicroserviceExample.git
```

### 2.3 Environment Configuration

The project ships with an example environment file. Copy it and leave the defaults in place for local development:

```bash
cp .env.example .env
```

The defaults work out of the box. The only time you need to change `.env` is if you expose the app to a network — in that case, change `JWT_SECRET_KEY` and `DEFAULT_ADMIN_PASSWORD` as instructed in the file.

> **Never commit your `.env` file.** It is already listed in `.gitignore`.

### 2.4 Running the Full Stack with Docker Compose

Docker Compose is the recommended way to run everything. One command starts all eight services, seven databases, RabbitMQ, Seq (log viewer), and the frontend:

```bash
docker compose up --build -d
```

The first run downloads images and builds containers — allow 3–5 minutes. Subsequent starts are fast.

**Check that everything came up healthy:**
```bash
docker compose ps
```
All services should show `healthy`. If any show `starting`, wait another 30 seconds and check again.

**Useful URLs once the stack is running:**

| URL | What it is |
|---|---|
| http://localhost:5173 | React frontend |
| http://localhost:5000/health | API gateway health (shows all downstream services) |
| http://localhost:5341 | Seq — structured log viewer (great for debugging) |
| http://localhost:15672 | RabbitMQ management UI (user: `guest`, pass: `guest`) |

**Default admin credentials** (from `.env`):

| Field | Value |
|---|---|
| Email | `admin@example.com` |
| Password | `Admin1234!` |

**Stopping the stack:**
```bash
docker compose down          # stop containers, keep data volumes
docker compose down -v       # stop containers AND delete all data (clean slate)
```

### 2.5 Running Services Locally (without Docker)

If you are working on a single service and want a faster edit-run-test cycle, you can run that service directly with `dotnet run` while the rest of the stack runs in Docker.

**Step 1 — Start the infrastructure services only:**
```bash
docker compose up -d postgres-auth postgres-user rabbitmq seq api-gateway
```

**Step 2 — Run your service:**
```bash
# Pick the service you are working on:
dotnet run --project AuthService/src/AuthService/
dotnet run --project UserManagementService/src/UserManagementService/
dotnet run --project AccountService/src/AccountService/
dotnet run --project ContactService/src/ContactService/
dotnet run --project DealService/src/DealService/
dotnet run --project ActivityService/src/ActivityService/
dotnet run --project ReportingService/src/ReportingService/
```

Local service ports (matching the gateway's routing config):

| Service | HTTP port |
|---|---|
| ApiGateway | 5000 |
| AuthService | 5188 |
| UserManagementService | 5151 |
| AccountService | 5243 |
| ContactService | 5167 |
| DealService | 5290 |
| ActivityService | 5291 |
| ReportingService | 5292 |

### 2.6 Running the Frontend

The frontend is a Vite dev server that proxies API calls through to the gateway:

```bash
cd frontend
npm install
npm run dev
```

Open http://localhost:5173. The Vite proxy is already configured to forward `/auth/*`, `/users/*`, `/contacts/*`, `/accounts/*`, `/deals/*`, `/pipeline/*`, `/activities/*`, and `/reports/*` to the gateway on port 5000.

### 2.7 Verifying Your Setup

Run the unit and integration test suites to confirm everything is wired up correctly:

```bash
# All unit and integration tests (~684 tests, ~2 minutes)
dotnet test MicroserviceExample.sln

# A single service if you want to move faster
dotnet test AuthService/src/AuthService.Tests/AuthService.Tests.csproj
```

All tests should pass. If any fail after a fresh clone, open an issue — that is a bug in the project, not your setup.

**To run the full E2E suite** (requires the Docker stack to be running):
```bash
dotnet test EndToEnd.Tests/EndToEnd.Tests.csproj
```

---

## 3. Finding Something to Work On

All open work is tracked as GitHub issues. Browse the [issue list](https://github.com/ScottsSecondAct/MicroserviceExample/issues) and filter by label:

| Label | Meaning |
|---|---|
| `v2.3`, `v2.4`, … | Which roadmap version the issue belongs to |
| `backend` | C# / ASP.NET Core service work |
| `frontend` | React / TypeScript / Tailwind UI work |
| `security` | Security hardening items (good for security-focused students) |
| `infrastructure` | Docker, CI, deployment, observability |
| `testing` | Test coverage or test tooling work |

**Good first issues** are the earlier-versioned ones (v2.3 and v2.4). They are smaller in scope, touch a single service, and have clear acceptance criteria. Issues in v3.x and v4.x tend to be larger cross-cutting features.

**Before you start work on an issue:**
1. Comment on the issue to say you are picking it up. This prevents two people building the same thing.
2. Read the full issue body — it includes implementation notes and acceptance criteria.
3. Cross-reference the relevant section in `ARCHITECTURE.md` and `ROADMAP.md`.

If you have a contribution idea that is not tracked as an issue, open one first and describe what you want to build. This avoids doing work that conflicts with something already in progress.

---

## 4. Workflow

### 4.1 Branching

Create a branch from the latest `main`:

```bash
git checkout main
git pull upstream main
git checkout -b feature/your-branch-name
```

Branch naming convention:

| Type | Pattern | Example |
|---|---|---|
| Feature | `feature/short-description` | `feature/pbkdf2-iteration-count` |
| Bug fix | `fix/short-description` | `fix/cors-policy-too-broad` |
| Documentation | `docs/short-description` | `docs/update-architecture-diagram` |

Keep branches focused. One issue per branch is the norm.

### 4.2 Making Changes

Build the solution frequently to catch compile errors early:
```bash
dotnet build MicroserviceExample.sln
```

Service and pattern conventions to follow (all in `ARCHITECTURE.md`):

- **Layered architecture:** Controller → IService → IRepository → DbContext. Do not skip layers.
- **ServiceResult pattern:** All service methods return `ServiceResult<T>`. See existing services for examples.
- **MassTransit:** Publish events via `IPublishEndpoint.Publish(...)`. Consumers implement `IConsumer<T>` and live in the service's `Consumers/` folder.
- **EF Core:** No lazy loading. Use `Include()` in repositories. No raw SQL unless there is a clear performance reason.
- **Fail-open HTTP clients:** `AccountClient`, `ContactClient`, and similar validation clients catch network exceptions and return a safe default rather than propagating the error.

### 4.3 Running Tests

Run the tests for the service you changed before pushing:

```bash
# Unit tests
dotnet test ServiceName/src/ServiceName.Tests/ServiceName.Tests.csproj

# Integration tests
dotnet test ServiceName/src/ServiceName.IntegrationTests/ServiceName.IntegrationTests.csproj

# Run a single test class
dotnet test --filter "FullyQualifiedName~ClassName" path/to/project.csproj
```

**Every pull request must include tests for the new code.** See [Section 6](#6-testing-requirements) for what is required.

### 4.4 Opening a Pull Request

Push your branch and open a PR against `main`:

```bash
git push origin feature/your-branch-name
```

Then go to GitHub and open the PR. In the PR description:
- Reference the issue number: `Closes #123`
- Briefly describe what you changed and why
- Note any decisions you made that are not obvious from the code
- List the tests you added

The CI pipeline runs automatically on every PR. It builds the solution and runs all unit and integration tests. **CI must be green before a reviewer will look at your PR.**

### 4.5 Code Review

A maintainer will review your PR and may leave comments requesting changes. This is normal and expected — code review is a learning opportunity, not a judgment. Respond to feedback by pushing additional commits to the same branch; do not open a new PR.

Once approved and CI passes, a maintainer will merge your PR.

---

## 5. Coding Standards

The project does not use an auto-formatter enforced in CI (yet), but follow these conventions to match the existing codebase:

**C#**
- 4-space indentation, no tabs
- PascalCase for types, methods, and properties; camelCase for local variables and parameters
- `var` for local variables when the type is obvious from the right-hand side
- Prefer `async`/`await` over `.Result` or `.Wait()`
- One class per file; file name matches class name
- `private readonly` fields injected via constructor, not property injection
- XML doc comments (`///`) are not required on internal classes

**React / JavaScript**
- Functional components only (no class components)
- `camelCase` for variables and functions; `PascalCase` for component names
- Use TanStack Query (`useQuery`, `useMutation`) for all server state — no `useEffect` + `fetch`
- shadcn/ui components for UI primitives (Dialog, Sheet, Toast, etc.)
- Tailwind utility classes for styling — no hand-written CSS files

**General**
- Do not add dead code, commented-out code, or `TODO` comments without a linked issue
- Do not introduce `Console.WriteLine` or `System.Diagnostics.Debug` in production code — use the injected `ILogger<T>`
- Do not commit secrets, credentials, or personal data

---

## 6. Testing Requirements

All PRs must maintain or improve test coverage. The current baseline is **97.2% line, 82.7% branch, 99.1% method**.

### What to add for a backend change

| What you changed | What you must add |
|---|---|
| A new service method | Unit tests for the happy path, not-found path, and any validation branches |
| A new controller action | Unit tests asserting the correct HTTP status code for success and each failure mode |
| A new repository method | Unit tests using EF Core InMemory (one test per filter combination) |
| A new MassTransit consumer | Unit test with a mocked service; integration test publishing to the harness |
| A new endpoint | Integration test covering the full HTTP pipeline with a real database |

### What to add for a frontend change

- Manual verification that the feature works in the browser is the minimum bar
- If the component has meaningful logic (form validation, state transitions), add a unit test with Vitest + Testing Library

### Testing patterns (quick reference)

```csharp
// Unit test — service with mocked repository
var mockRepo = new Mock<IContactRepository>();
var service = new ContactService(mockRepo.Object, /* other deps */);
var result = await service.GetContactAsync(contactId);
result.StatusCode.Should().Be(200);

// Integration test — real DB via Testcontainer
public class ContactTests : IClassFixture<ContactServiceFactory>
{
    private readonly HttpClient _client;
    public ContactTests(ContactServiceFactory factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task CreateContact_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/contacts", new { ... });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
```

See [`TESTING.md`](TESTING.md) for full patterns including MassTransit harness usage and WireMock stubs for downstream services.

---

## 7. Project Documentation to Read First

| Document | Purpose |
|---|---|
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | Service map, database assignments, event inventory, shared library breakdown |
| [`ROADMAP.md`](ROADMAP.md) | What is built and what is planned, version by version |
| [`TESTING.md`](TESTING.md) | Three-layer test strategy with full code examples |
| [`WORKFLOWS.md`](WORKFLOWS.md) | CI/CD pipelines and the branch → PR → merge → release flow |
| [`USERS_MANUAL.md`](USERS_MANUAL.md) | End-user perspective on the application — useful for understanding what you are building |
| [`SECURITY_VULNERABILITIES.md`](SECURITY_VULNERABILITIES.md) | Known issues and their planned fixes — context for v2.3 security issues |

---

## 8. Getting Help

**Stuck on setup?** Open a [GitHub issue](https://github.com/ScottsSecondAct/MicroserviceExample/issues) with the tag `question` and describe what you tried and what happened.

**Unsure about an implementation approach?** Comment on the relevant issue before writing code. It is better to align early than to build something that needs to be re-done.

**Found a bug in the existing code?** Open a new issue with a description of the expected vs. actual behavior, your OS, .NET version, and Docker version.

**Helpful resources for the tech stack:**

| Topic | Resource |
|---|---|
| ASP.NET Core | https://learn.microsoft.com/en-us/aspnet/core |
| Entity Framework Core | https://learn.microsoft.com/en-us/ef/core |
| MassTransit | https://masstransit.io/documentation |
| xUnit | https://xunit.net/docs/getting-started/netcore/cmdline |
| React + TanStack Query | https://tanstack.com/query/latest/docs/framework/react/overview |
| Tailwind CSS | https://tailwindcss.com/docs |
| shadcn/ui | https://ui.shadcn.com/docs |
| Docker Compose | https://docs.docker.com/compose |

---

## 9. Code of Conduct

This project follows the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md). All contributors — regardless of experience level — are expected to treat each other with respect. Please read it before participating.

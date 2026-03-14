# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.6.x   | :white_check_mark: |
| < 1.6   | :x:                |

## Reporting a Vulnerability

**Please do not open a public GitHub issue for security vulnerabilities.**

Instead, report vulnerabilities privately using one of the following methods:

1. **GitHub Private Vulnerability Reporting:** Use the [Security Advisories](https://github.com/ScottsSecondAct/MicroserviceExample/security/advisories/new) page to submit a private report directly on GitHub.
2. **Email:** Send details to **scott@ScottsSecondAct.com**.

### What to Include

- Description of the vulnerability
- Steps to reproduce
- Affected version(s)
- Potential impact

### What to Expect

- **Acknowledgment** within 72 hours of your report
- **Status update** within 7 days with an initial assessment
- **Resolution timeline** communicated once the issue is confirmed
- Credit in the release notes (unless you prefer to remain anonymous)

### Scope

MicroserviceExample is a distributed ASP.NET Core CRM system. Relevant security concerns include:

- Authentication and authorization bypass (JWT validation, gateway auth policy)
- Injection attacks against API endpoints (SQL injection via EF Core, command injection)
- Secrets exposure (JWT keys, database credentials, RabbitMQ credentials in config or logs)
- Cross-service privilege escalation (bypassing the gateway to reach unauthenticated downstream services)
- Unsafe handling of user-supplied data in any service

A detailed inventory of known security gaps between this codebase and production readiness is documented in [SECURITY_VULNERABILITIES.md](SECURITY_VULNERABILITIES.md).

### Out of Scope

- Issues requiring physical access to the machine
- Social engineering
- Vulnerabilities in dependencies with existing upstream fixes (please check first)
- Known issues already documented in SECURITY_VULNERABILITIES.md

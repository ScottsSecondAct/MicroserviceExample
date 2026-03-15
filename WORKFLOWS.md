# Development Workflow

This document describes how engineers interact with the three CI/CD pipelines and the overall flow from writing code to shipping a release.

## Pipelines at a Glance

| Pipeline | Trigger | What it does |
|---|---|---|
| **CI** | Push to any branch except `main`; every PR targeting `main` | Build + unit + integration tests |
| **Staging** | Merge to `main` | Build + unit + integration tests → full E2E suite |
| **Release** | Push of a `v*` tag | Creates the GitHub Release with auto-generated notes |

---

## Day-to-Day Flow

### 1. Work on a feature branch

Create a branch from `main` and push commits as you work:

```sh
git checkout -b feature/my-feature
# ... make changes ...
git push origin feature/my-feature
```

The **CI pipeline** runs automatically on every push. It builds the solution and runs all unit and integration tests. Typical runtime is 1–2 minutes.

**If CI is red, fix it before requesting a review.** A failing CI build is a signal that something is broken, not a state to ignore.

---

### 2. Open a pull request

When the feature is ready, open a PR targeting `main` on GitHub. CI runs again on the PR.

The `main` branch is protected — **a PR cannot be merged until:**
- The CI status check passes
- At least one approving review is received

This ensures that nothing lands on `main` without passing tests and a code review.

---

### 3. Merge to main

Once CI is green and the PR is approved, merge it. The **Staging pipeline** fires automatically.

Staging runs unit and integration tests, then spins up the full Docker Compose stack and runs the E2E suite against live services. This is the authoritative gate:

- It catches integration problems that unit tests can't see
- It verifies the services work together end-to-end before anything reaches production

**If Staging is red, `main` is broken.** Fixing it becomes the team's top priority — no new releases should be cut until `main` is green again.

---

### 4. Cut a release

When `main` is stable and the team is ready to ship, tag the commit with a version number following [Semantic Versioning](https://semver.org):

```sh
git checkout main
git pull
git tag v2.1.0
git push origin v2.1.0
```

The **Release pipeline** fires, creates a GitHub Release, and auto-generates release notes from the PR titles merged since the last tag.

No tests run in the release pipeline — the commit being tagged has already been fully validated by CI (on the feature branch) and Staging (on merge to `main`). Tagging promotes an already-verified commit; it does not re-verify it.

---

## Branch Protection Rules

The pipelines enforce nothing without branch protection configured. The following rules are set on `main` in *Settings → Branches*:

- **Require status checks to pass before merging** — CI must be green
- **Require at least one approving review** — a teammate must review the PR
- **Require branches to be up to date before merging** — the PR must be rebased or merged with the latest `main` before it can land

Without these rules, engineers can merge a PR even when CI is red, bypassing the entire gate.

---

## Pipeline Files

| File | Description |
|---|---|
| `.github/workflows/ci.yml` | CI pipeline — build and unit/integration tests |
| `.github/workflows/staging.yml` | Staging pipeline — build, unit/integration tests, and E2E |
| `.github/workflows/release.yml` | Release pipeline — create GitHub Release |
| `scripts/run-e2e.sh` | E2E runner — starts the Docker stack, waits for health, runs tests, tears down |

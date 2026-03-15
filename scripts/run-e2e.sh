#!/usr/bin/env bash
# ---------------------------------------------------------------------------
# run-e2e.sh — Start the Docker Compose stack, run all E2E tests, tear down.
#
# Usage:
#   ./scripts/run-e2e.sh
#
# Environment variables (all optional — safe defaults used if absent):
#   E2E_GATEWAY_URL        Gateway base URL  (default: http://localhost:5000)
#   E2E_STARTUP_TIMEOUT    Seconds to wait for the gateway health check (default: 120)
#   JWT_SECRET             (default: a fixed 32-char test secret)
#   DEFAULT_ADMIN_PASSWORD (default: Admin1234!)
#   RABBITMQ_USERNAME      (default: guest)
#   RABBITMQ_PASSWORD      (default: guest)
#   AUTH_DB_PASSWORD       (default: test-auth-pass)
#   USER_DB_PASSWORD       (default: test-user-pass)
#   ACCOUNT_DB_PASSWORD    (default: test-account-pass)
#   CONTACT_DB_PASSWORD    (default: test-contact-pass)
#   DEAL_DB_PASSWORD       (default: test-deal-pass)
#   ACTIVITY_DB_PASSWORD   (default: test-activity-pass)
#   REPORTING_DB_PASSWORD  (default: test-reporting-pass)
#
# In CI, either let the defaults take effect or export variables before
# calling this script.  A .env file in the repo root is also respected by
# docker compose when it exists.
# ---------------------------------------------------------------------------
set -euo pipefail

GATEWAY_URL="${E2E_GATEWAY_URL:-http://localhost:5000}"
STARTUP_TIMEOUT="${E2E_STARTUP_TIMEOUT:-120}"

# Apply safe test defaults for every variable docker-compose.yml requires,
# but only when the variable is not already set in the environment.
export JWT_SECRET="${JWT_SECRET:-e2e-test-secret-minimum-32-characters-ok}"
export DEFAULT_ADMIN_PASSWORD="${DEFAULT_ADMIN_PASSWORD:-Admin1234!}"
export RABBITMQ_USERNAME="${RABBITMQ_USERNAME:-guest}"
export RABBITMQ_PASSWORD="${RABBITMQ_PASSWORD:-guest}"
export AUTH_DB_PASSWORD="${AUTH_DB_PASSWORD:-test-auth-pass}"
export USER_DB_PASSWORD="${USER_DB_PASSWORD:-test-user-pass}"
export ACCOUNT_DB_PASSWORD="${ACCOUNT_DB_PASSWORD:-test-account-pass}"
export CONTACT_DB_PASSWORD="${CONTACT_DB_PASSWORD:-test-contact-pass}"
export DEAL_DB_PASSWORD="${DEAL_DB_PASSWORD:-test-deal-pass}"
export ACTIVITY_DB_PASSWORD="${ACTIVITY_DB_PASSWORD:-test-activity-pass}"
export REPORTING_DB_PASSWORD="${REPORTING_DB_PASSWORD:-test-reporting-pass}"

# Move to the repository root regardless of where the script is called from.
cd "$(dirname "$0")/.."

echo "==> Starting Docker Compose stack..."
docker compose up --build -d

echo "==> Waiting for gateway health check at ${GATEWAY_URL}/health (timeout: ${STARTUP_TIMEOUT}s)..."
deadline=$((SECONDS + STARTUP_TIMEOUT))
until curl -sf "${GATEWAY_URL}/health" > /dev/null; do
  if (( SECONDS > deadline )); then
    echo "ERROR: Gateway did not become healthy within ${STARTUP_TIMEOUT}s."
    echo "--- api-gateway logs ---"
    docker compose logs --tail=50 api-gateway
    docker compose down -v
    exit 1
  fi
  sleep 2
done
echo "==> Gateway is healthy."

echo "==> Running E2E tests..."
E2E_GATEWAY_URL="${GATEWAY_URL}" dotnet test EndToEnd.Tests/EndToEnd.Tests.csproj --verbosity normal
TEST_EXIT=$?

echo "==> Tearing down Docker Compose stack..."
docker compose down -v

exit $TEST_EXIT

# Frontend API Coverage Audit (updated March 2026)

Audit conducted against all 7 services + gateway. 44 unique backend endpoints identified; frontend coverage assessed by reading all API client modules and page components in `frontend/src/`.

## Coverage by service

| Service | Endpoints | Called by Frontend | Coverage |
|---------|-----------|-------------------|----------|
| AuthService | 9 | 9 | **100%** |
| UserManagementService | 8 | 8 | **100%** |
| ContactService | 5 | 5 | **100%** |
| AccountService | 5 | 5 | **100%** |
| DealService | 8 | 8 | **100%** |
| ActivityService | 4 | 4 | **100%** |
| ReportingService | 4 | 4 | **100%** |
| **Total** | **43** | **43** | **100%** |

## Endpoints by service

### AuthService

| Method | Endpoint | Frontend Call |
|--------|----------|---------------|
| POST | `/api/login/login` | `authApi.login()` |
| POST | `/api/login/refresh` | `authApi.refresh()` — called automatically by `AuthContext` before JWT expiry |
| GET | `/api/login/me` | `authApi.me()` |
| POST | `/api/registration/register` | `authApi.register()` |
| POST | `/api/registration/accept-invite` | `authApi.acceptInvite()` |
| POST | `/api/auth/change-password` | `authApi.changePassword()` |
| POST | `/api/auth/forgot-password` | `authApi.forgotPassword()` |
| POST | `/api/auth/reset-password` | `authApi.resetPassword()` |
| POST | `/api/users/invite` | `adminApi.inviteUser()` |

Note: `POST /api/tenants/provision` is an ops-only bootstrap endpoint (requires `X-Bootstrap-Secret` header); it has no user-facing flow and is intentionally excluded.

### UserManagementService

| Method | Endpoint | Frontend Call |
|--------|----------|---------------|
| GET | `/api/users/{userId}` | `usersApi.getProfile()` |
| GET | `/api/users/team` | `usersApi.getTeam()` |
| GET | `/api/users/{userId}/role` | Internal — called by AuthService on login/refresh via `IUserRoleClient`; not a frontend endpoint |
| POST | `/api/users/{userId}/resend-invite` | `adminApi.resendInvite()` |
| GET | `/api/users/audit` | `usersApi.getAuditLog()` — Admin Audit Log page |
| GET | `/api/admin/users` | `adminApi.listUsers()` |
| PUT | `/api/admin/users/{userId}/role` | `adminApi.updateRole()` |
| PUT | `/api/admin/users/{userId}/active` | `adminApi.setActive()` |

Note: `POST /api/users`, `PATCH /api/users/{id}/status`, and `PATCH /api/users/{id}/role` were removed — the POST was made redundant by async registration via RabbitMQ; the PATCH routes were shadowed by the admin equivalents.

### ContactService

| Method | Endpoint | Frontend Call |
|--------|----------|---------------|
| GET | `/api/contacts` | `contactsApi.list()` |
| GET | `/api/contacts/{id}` | `contactsApi.get()` |
| POST | `/api/contacts` | `contactsApi.create()` |
| PUT | `/api/contacts/{id}` | `contactsApi.update()` |
| DELETE | `/api/contacts/{id}` | `contactsApi.delete()` |

### AccountService

| Method | Endpoint | Frontend Call |
|--------|----------|---------------|
| GET | `/api/accounts` | `accountsApi.list()` |
| GET | `/api/accounts/{id}` | `accountsApi.get()` |
| POST | `/api/accounts` | `accountsApi.create()` |
| PUT | `/api/accounts/{id}` | `accountsApi.update()` |
| DELETE | `/api/accounts/{id}` | `accountsApi.delete()` |

### DealService

| Method | Endpoint | Frontend Call |
|--------|----------|---------------|
| GET | `/api/deals` | `dealsApi.list()` |
| GET | `/api/deals/{id}` | `dealsApi.get()` |
| POST | `/api/deals` | `dealsApi.create()` |
| PUT | `/api/deals/{id}` | `dealsApi.update()` |
| DELETE | `/api/deals/{id}` | `dealsApi.delete()` |
| POST | `/api/deals/{id}/contacts` | `dealsApi.addContact()` |
| DELETE | `/api/deals/{id}/contacts/{contactId}` | `dealsApi.removeContact()` |
| GET | `/api/pipeline` | `dealsApi.getPipeline()` |

### ActivityService

| Method | Endpoint | Frontend Call |
|--------|----------|---------------|
| GET | `/api/activities` | `activitiesApi.list()` — used by ActivityTimeline and TaskList |
| POST | `/api/activities` | `activitiesApi.create()` — ActivityLogForm on ContactDetail, AccountDetail, DealDetail |
| PUT | `/api/activities/{id}` | `activitiesApi.update()` — ActivityTimeline (complete task) and TaskList |
| DELETE | `/api/activities/{id}` | `activitiesApi.delete()` — ActivityTimeline and TaskList |

Note: `GET /api/activities/{id}` was removed — all activity interaction is inline via ActivityTimeline (list, complete, delete) and ActivityLogForm (create); a detail endpoint adds no UI value.

### ReportingService

| Method | Endpoint | Frontend Call |
|--------|----------|---------------|
| GET | `/api/reports/pipeline` | `reportsApi.pipeline()` |
| GET | `/api/reports/activities` | `reportsApi.activities()` |
| GET | `/api/reports/contacts` | `reportsApi.contacts()` |
| GET | `/api/reports/dashboard` | `reportsApi.dashboard()` |

## Frontend pages and their API calls

| Page | API Calls |
|------|-----------|
| Login.jsx | `authApi.login()` |
| Register.jsx | `authApi.register()` |
| ForgotPassword.jsx | `authApi.forgotPassword()` |
| ResetPassword.jsx | `authApi.resetPassword()` |
| ChangePassword.jsx | `authApi.changePassword()` |
| AcceptInvite.jsx | `authApi.acceptInvite()` |
| Profile.jsx | `authApi.me()`, `usersApi.getProfile()` |
| AdminUserList.jsx | `adminApi.listUsers/updateRole/setActive`, `authApi.inviteUser`, `adminApi.resendInvite` |
| AuditLog.jsx | `usersApi.getAuditLog()` |
| AccountList.jsx | `accountsApi.list()`, `delete()` |
| AccountForm.jsx | `accountsApi.get/create/update()` |
| AccountDetail.jsx | `accountsApi.get/delete()`, `contactsApi.list()` (filtered by accountId), `activitiesApi.list/create/update/delete()` (via ActivityTimeline + ActivityLogForm) |
| ContactList.jsx | `contactsApi.list()` (with filters), `delete()`, bulk status update, `usersApi.getTeam()` |
| ContactForm.jsx | `contactsApi.get/create/update()`, `accountsApi.list()`, `usersApi.getTeam()` |
| ContactDetail.jsx | `contactsApi.get/delete/update()`, `accountsApi.get()`, `usersApi.getTeam()`, `activitiesApi.list/create/update/delete()` (via ActivityTimeline + ActivityLogForm) |
| Pipeline.jsx | `dealsApi.getPipeline()`, update stage |
| DealForm.jsx | `dealsApi.get/create/update()`, `accountsApi.list()`, `usersApi.getTeam()` |
| DealDetail.jsx | `dealsApi.get/update/delete/addContact/removeContact()`, `contactsApi.list()`, `accountsApi.get()`, `activitiesApi.list/create/update/delete()` (via ActivityTimeline + ActivityLogForm) |
| TaskList.jsx | `activitiesApi.list()` (Tasks only), `update()`, `delete()` |
| Dashboard.jsx | `reportsApi.dashboard/pipeline/activities/contacts()` |

## Token refresh

`AuthContext` stores both the JWT and refresh token in `localStorage`. On login (and on page load if a token exists), it schedules a `setTimeout` to fire 5 minutes before the JWT expires. The timer calls `authApi.refresh()`, which rotates the refresh token and issues a new JWT. On refresh failure the user is logged out. Sessions are now continuous for as long as the refresh token is valid.

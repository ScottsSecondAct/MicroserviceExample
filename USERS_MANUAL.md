# Users Manual

## Table of Contents

1. [Introduction](#1-introduction)
2. [Installation](#2-installation)
   - 2.1 [System Requirements](#21-system-requirements)
   - 2.2 [Docker Compose Setup](#22-docker-compose-setup)
   - 2.3 [Environment Configuration](#23-environment-configuration)
   - 2.4 [Admin Account Setup](#24-admin-account-setup)
   - 2.5 [Verifying the Installation](#25-verifying-the-installation)
3. [Logging In](#3-logging-in)
   - 3.1 [First Login](#31-first-login)
   - 3.2 [Forgot Password](#32-forgot-password)
4. [Navigation Overview](#4-navigation-overview)
5. [User Provisioning](#5-user-provisioning)
   - 5.1 [Inviting a New User](#51-inviting-a-new-user)
   - 5.2 [Accepting an Invite (User Experience)](#52-accepting-an-invite-user-experience)
   - 5.3 [Monitoring Pending Invites](#53-monitoring-pending-invites)
   - 5.4 [Resending an Invite](#54-resending-an-invite)
   - 5.5 [Assigning Roles](#55-assigning-roles)
   - 5.6 [Deactivating a User](#56-deactivating-a-user)
   - 5.7 [Reactivating a User](#57-reactivating-a-user)
6. [Contacts](#6-contacts)
   - 6.1 [Viewing Contacts](#61-viewing-contacts)
   - 6.2 [Creating a Contact](#62-creating-a-contact)
   - 6.3 [Editing and Deleting Contacts](#63-editing-and-deleting-contacts)
   - 6.4 [Contact Lifecycle Statuses](#64-contact-lifecycle-statuses)
7. [Accounts](#7-accounts)
   - 7.1 [Viewing Accounts](#71-viewing-accounts)
   - 7.2 [Creating an Account](#72-creating-an-account)
   - 7.3 [Account Detail View](#73-account-detail-view)
8. [Pipeline (Deals)](#8-pipeline-deals)
   - 8.1 [Kanban Board Overview](#81-kanban-board-overview)
   - 8.2 [Creating a Deal](#82-creating-a-deal)
   - 8.3 [Moving a Deal Through Stages](#83-moving-a-deal-through-stages)
   - 8.4 [Deal Detail View](#84-deal-detail-view)
9. [Tasks](#9-tasks)
   - 9.1 [Viewing Tasks](#91-viewing-tasks)
   - 9.2 [Completing a Task](#92-completing-a-task)
10. [Dashboard](#10-dashboard)
11. [Profile](#11-profile)
12. [Roles and Permissions](#12-roles-and-permissions)
13. [Appendix A: Password Policy](#appendix-a-password-policy)
14. [Appendix B: Troubleshooting](#appendix-b-troubleshooting)

---

## 1. Introduction

This manual covers the day-to-day use of the CRM application, including installation, user management, and all core CRM workflows: contacts, accounts, deals, activities, and reporting.

The application is a multi-service web platform accessed through a browser. No software needs to be installed on individual user machines beyond a modern web browser.

---

## 2. Installation

### 2.1 System Requirements

| Requirement | Minimum |
|---|---|
| Docker Engine | 24.0 or later |
| Docker Compose | v2.20 or later |
| RAM | 4 GB available |
| Disk | 10 GB free |
| OS | Linux, macOS, or Windows (WSL2) |

An outbound internet connection is required on first run to pull Docker images.

### 2.2 Docker Compose Setup

1. Clone the repository or extract the release archive:

   ```bash
   git clone https://github.com/your-org/MicroserviceExample.git
   cd MicroserviceExample
   ```

2. Copy the environment template:

   ```bash
   cp .env.example .env
   ```

3. Start all services:

   ```bash
   docker compose up -d
   ```

4. Wait for all containers to report healthy (typically 30–60 seconds on first start):

   ```bash
   docker compose ps
   ```

   All services should show `healthy` under the Status column.

5. Open the application in a browser:

   ```
   http://localhost:5173
   ```

### 2.3 Environment Configuration

The `.env` file controls all runtime settings. Key values to review before going live:

| Variable | Description | Default |
|---|---|---|
| `JWT_SECRET_KEY` | Secret used to sign authentication tokens. **Change before production.** | `dev-secret-key-change-me` |
| `DEFAULT_ADMIN_EMAIL` | Email address of the built-in admin account | `admin@example.com` |
| `DEFAULT_ADMIN_PASSWORD` | Initial password for the admin account | `Admin1234!` |
| `SMTP_HOST` | Outbound mail server hostname | `mailhog` (local dev only) |
| `SMTP_PORT` | Outbound mail server port | `1025` |
| `FRONTEND_URL` | Base URL sent in invite and password-reset emails | `http://localhost:5173` |

> **Security note:** Change `JWT_SECRET_KEY` and `DEFAULT_ADMIN_PASSWORD` before exposing the application to any network. Use a randomly generated string of at least 32 characters for the JWT secret.

### 2.4 Admin Account Setup

The application ships with a single built-in administrator account. This account is created automatically on first startup using the values from `.env`.

**Default credentials:**

| Field | Value |
|---|---|
| Email | `admin@example.com` (configurable via `DEFAULT_ADMIN_EMAIL`) |
| Password | `Admin1234!` (configurable via `DEFAULT_ADMIN_PASSWORD`) |

On first login you will be prompted to change the password. The new password must satisfy the password policy (see [Appendix A](#appendix-a-password-policy)).

**Recommended steps after installation:**

1. Log in with the default credentials.
2. Change the admin password when prompted.
3. Navigate to **Admin > Users** and invite any additional administrators or team members.
4. Set roles for each user after they accept their invite.
5. Update `DEFAULT_ADMIN_EMAIL` in `.env` to a real mailbox if you need password-reset emails to reach the admin.

> **Note for on-premises deployments:** The built-in admin account cannot be deleted. It serves as the break-glass recovery account if all other admin accounts are lost. Store the credentials securely.

### 2.5 Verifying the Installation

After startup, confirm the following endpoints are reachable:

| URL | Expected result |
|---|---|
| `http://localhost:5173` | React frontend loads |
| `http://localhost:5000/auth/health` | `{"status":"Healthy"}` |
| `http://localhost:5000/users/health` | `{"status":"Healthy"}` |
| `http://localhost:8025` | MailHog web UI (dev only, for viewing invite emails) |

---

## 3. Logging In

### 3.1 First Login

1. Navigate to `http://localhost:5173` (or the configured public URL).
2. Enter your email address and password.
3. Click **Sign in**.
4. If your account was provisioned by an administrator and requires a password change, you will be redirected to the **Change Password** page automatically. Enter a new password that meets the password policy and click **Update Password**.

After a successful login you will land on the **Dashboard**.

### 3.2 Forgot Password

1. On the Login page, click **Forgot password?**
2. Enter your registered email address and click **Send Reset Link**.
3. Check your inbox for a message with a reset link. The link expires after 24 hours.
4. Click the link, enter a new password, and click **Reset Password**.
5. You will be redirected to the Login page.

---

## 4. Navigation Overview

The main navigation is displayed in a sidebar on the left. Items are grouped by function:

| Group | Pages | Minimum role |
|---|---|---|
| **CRM** | Contacts | Member |
| | Accounts | Member |
| | Pipeline | Member |
| **Productivity** | Tasks | Member |
| **Insights** | Dashboard | Member |
| **Admin** | Users | Admin only |

Your profile icon appears at the bottom of the sidebar. Click it to access **Profile** settings or to **Sign out**.

---

## 5. User Provisioning

All user provisioning is performed by an administrator from the **Admin > Users** page. Users cannot self-register; every account must be created through an invite.

### 5.1 Inviting a New User

1. Log in with an Admin account.
2. In the sidebar, click **Admin > Users**.
3. Click the **Invite User** button (top-right of the page).
4. Enter the user's email address in the dialog and click **Send Invite**.

The system will:
- Create a placeholder profile for the user.
- Send an invitation email to the address provided.
- Show the user in the list with an amber **Invite pending** badge.

Invite tokens expire after **48 hours**. If the user does not accept within that window, resend the invite (see [Section 5.4](#54-resending-an-invite)).

### 5.2 Accepting an Invite (User Experience)

The invited user receives an email with a subject line such as **"You've been invited"**. The email contains a link of the form:

```
http://<your-domain>/accept-invite?token=<unique-token>
```

The user must:

1. Click the link in the email (or copy-paste it into a browser).
2. On the **Accept Invite** page, enter a password that satisfies the password policy.
3. Click **Set Password**.
4. They will be redirected to the Login page and can log in immediately.

> **Important:** Each invite link is single-use and expires after 48 hours. A new invite must be sent if the link expires or is lost.

After the user accepts the invite, their profile is activated and the **Invite pending** badge is removed from the admin user list.

### 5.3 Monitoring Pending Invites

On the **Admin > Users** page, any user who has been invited but has not yet accepted is shown with an amber **Invite pending** badge next to their name. The date the invite was sent is also recorded.

Use this view to identify invites that may need to be resent.

### 5.4 Resending an Invite

1. On the **Admin > Users** page, find the user with the **Invite pending** badge.
2. Click the **Resend invite** button in that user's row.
3. A new invite token is generated, the old one is invalidated, and a fresh email is sent.

### 5.5 Assigning Roles

All users are added through the admin invite flow. Invited users start with the **Unassigned** role and have no CRM access until an administrator promotes them after they accept the invite.

To change a user's role:

1. On the **Admin > Users** page, find the user.
2. Use the role dropdown in their row to select the desired role.
3. The change takes effect immediately. The user's next page load or login will reflect the new role.

> **Note:** Administrators cannot set a user's role back to **Unassigned**. It is a system-assigned holding state, not an assignable role.

Available roles and their access level:

| Role | Description |
|---|---|
| **Unassigned** | Holding state for invited users who have not yet accepted their invite. No CRM access. Cannot be assigned by an administrator. |
| **Member** | Read-only access to CRM data. Can view contacts, accounts, deals, activities, and dashboard. |
| **SalesRep** | Full CRM access. Can create and edit contacts, accounts, deals, and activities. |
| **Manager** | All SalesRep permissions. Can view team-wide reporting. |
| **Admin** | Full access including **Admin > Users**. Can invite, manage roles, deactivate, and reactivate users. |

### 5.6 Deactivating a User

Deactivating a user prevents them from logging in without deleting their data or history.

1. On the **Admin > Users** page, find the user.
2. Click the **Deactivate** toggle or button in their row.
3. Confirm the action in the dialog.

The user will receive an "Account inactive" error on their next login attempt.

### 5.7 Reactivating a User

1. On the **Admin > Users** page, find the deactivated user (shown with an inactive indicator).
2. Click the **Activate** toggle or button.

The user can log in again immediately.

---

## 6. Contacts

### 6.1 Viewing Contacts

Navigate to **CRM > Contacts**. The page shows a table of all contacts with columns for name, email, status, account, and assigned owner. Use the status filter dropdown at the top to narrow the list.

Click a contact's name to open the **Contact Detail** page.

### 6.2 Creating a Contact

1. On the Contacts page, click **Add Contact**.
2. A slide-out panel opens. Fill in the required fields:
   - **First Name / Last Name**
   - **Email**
3. Optional fields: Phone, Title, Account (link to a company), Owner (team member), Status.
4. Click **Save**.

### 6.3 Editing and Deleting Contacts

- **Edit:** Click the pencil icon in the contact's row, update fields in the slide-out panel, and click **Save**.
- **Delete:** Click the trash icon in the contact's row and confirm. Deletion also removes associated deal-contact links.

### 6.4 Contact Lifecycle Statuses

| Status | Meaning |
|---|---|
| **Lead** | Initial state. Contact has not been qualified. |
| **Prospect** | Contact has been reviewed and is worth pursuing. |
| **Customer** | Contact has converted. |
| **Churned** | Former customer who has left. |

---

## 7. Accounts

### 7.1 Viewing Accounts

Navigate to **CRM > Accounts**. The list shows all company accounts with name, industry, and website.

### 7.2 Creating an Account

1. On the Accounts page, click **Add Account**.
2. Fill in **Company Name** (required). Optional: Industry, Website, Phone, Address.
3. Click **Save**.

### 7.3 Account Detail View

Click an account name to view:
- Company details and firmographics
- Associated contacts
- Associated deals

---

## 8. Pipeline (Deals)

### 8.1 Kanban Board Overview

Navigate to **CRM > Pipeline**. Deals are displayed in a Kanban board with five columns:

| Stage | Meaning |
|---|---|
| **Prospecting** | Early-stage, exploratory discussion |
| **Proposal** | Formal proposal or quote sent |
| **Negotiation** | Terms being agreed |
| **Closed Won** | Deal successfully closed |
| **Closed Lost** | Deal lost or abandoned |

Each card shows the deal name and value. The column header shows the total value of all deals in that stage.

### 8.2 Creating a Deal

1. Click **Add Deal** (top-right of the Pipeline page).
2. Fill in the required fields: **Deal Name**, **Value**, **Stage**.
3. Optional: link to a **Contact** and/or **Account**.
4. Click **Save**.

### 8.3 Moving a Deal Through Stages

Drag a deal card from one column and drop it into the destination column. The stage is updated immediately.

### 8.4 Deal Detail View

Click a deal card to open the **Deal Detail** page, which shows full deal information, associated contacts, and the activity timeline for that deal.

---

## 9. Tasks

### 9.1 Viewing Tasks

Navigate to **Productivity > Tasks**. This page lists all activity records of type **Task** that are assigned to you and have not yet been completed.

### 9.2 Completing a Task

Click the **Complete** button on a task row. The task is marked done and disappears from the list. Completed tasks appear in the associated contact or deal's activity timeline.

---

## 10. Dashboard

Navigate to **Insights > Dashboard** to view aggregated reporting metrics:

- **Pipeline by Stage** — total deal value in each pipeline stage
- **Activity by Rep** — activity count per team member
- **Contact Funnel** — contact counts by lifecycle status

Data is updated in near real-time as deals, contacts, and activities are created or modified.

---

## 11. Profile

Click your avatar or initials at the bottom of the sidebar to open the **Profile** page. From here you can:

- Update your **Display Name**
- Change your **Password**

To change your password:
1. Enter your current password.
2. Enter the new password (must meet the password policy).
3. Click **Save**.

---

## 12. Roles and Permissions

| Feature | Unassigned | Member | SalesRep | Manager | Admin |
|---|:---:|:---:|:---:|:---:|:---:|
| Log in | Yes | Yes | Yes | Yes | Yes |
| View Contacts / Accounts / Deals | No | Yes | Yes | Yes | Yes |
| Create / Edit Contacts, Accounts, Deals | No | No | Yes | Yes | Yes |
| Delete Contacts, Accounts, Deals | No | No | Yes | Yes | Yes |
| View Tasks | No | Yes | Yes | Yes | Yes |
| View Dashboard | No | Yes | Yes | Yes | Yes |
| View team-wide reporting | No | No | No | Yes | Yes |
| Invite / manage users | No | No | No | No | Yes |
| Assign roles | No | No | No | No | Yes |
| Deactivate / reactivate users | No | No | No | No | Yes |

---

## Appendix A: Password Policy

All passwords must meet the following requirements:

- At least **8 characters** long
- At least one **uppercase letter** (A–Z)
- At least one **lowercase letter** (a–z)
- At least one **digit** (0–9)
- At least one **special character** (e.g. `!`, `@`, `#`, `$`, `%`)

Passwords that do not meet these requirements will be rejected with a specific error message indicating which requirement was not satisfied.

---

## Appendix B: Troubleshooting

**I never received an invite email.**
- Check your spam/junk folder.
- Ask your administrator to use the **Resend invite** button on the Admin > Users page.
- If the invite token is older than 48 hours it has expired; resending will generate a fresh link.
- In development environments, invite emails are captured by MailHog at `http://localhost:8025` and are not delivered externally.

**I clicked the invite link but got "Invalid or expired token".**
- The link has expired (48-hour limit) or has already been used. Ask your administrator to resend the invite.

**My password reset link is not working.**
- Reset links expire after 24 hours. Request a new link from the Forgot Password page.

**I can log in but I cannot see any CRM pages.**
- Your role is likely **Unassigned**. Contact an administrator to assign you an appropriate role.

**A service shows "Unhealthy" in `docker compose ps`.**
- Run `docker compose logs <service-name>` to see error details.
- Common causes: database not yet ready on first boot (retry after 30 seconds), or a missing environment variable.

**The application is unreachable after a server restart.**
- Run `docker compose up -d` to bring services back up. Services do not start automatically on host reboot unless configured with `restart: always` in `docker-compose.yml`.

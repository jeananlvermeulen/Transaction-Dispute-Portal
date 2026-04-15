# Capitec Transaction Dispute Portal

A full-stack, production-grade web application that allows Capitec Bank customers to view their transactions, raise disputes, and track outcomes — while giving Capitec employees a dedicated portal to manage and resolve those disputes.

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Architecture Overview](#architecture-overview)
3. [Technology Stack & Design Decisions](#technology-stack--design-decisions)
4. [Project Structure](#project-structure)
5. [Before You Start — SMTP Setup](#before-you-start--smtp-setup)
6. [Prerequisites](#prerequisites)
7. [Running the Project](#running-the-project)
   - [Option A: Local Development](#option-a-local-development)
   - [Option B: Docker (Full Stack)](#option-b-docker-full-stack)
8. [Configuration & Secrets](#configuration--secrets)
9. [Using the Application](#using-the-application)
   - [Customer Portal](#customer-portal)
   - [Employee Portal](#employee-portal)
10. [API Documentation (Swagger)](#api-documentation-swagger)
11. [Security Features](#security-features)
12. [Email Notifications](#email-notifications)
13. [Testing](#testing)
14. [Logging & Monitoring](#logging--monitoring)
15. [Future Enhancements](#future-enhancements)

---

## Project Overview

The Capitec Transaction Dispute Portal solves a real-world banking problem: giving customers a transparent, trackable way to dispute unauthorised or incorrect transactions, while giving bank employees the tools to investigate and resolve those disputes efficiently.

**Key capabilities:**

| Feature | Detail |
|---|---|
| Customer registration & login | Email verification, strong password rules, MFA via authenticator app |
| Dispute submission | Reason selection, free-text summary, auto-translation to English |
| Dispute tracking | Live status updates, full status history with employee notes |
| Employee management | Separate employee portal, dispute queue, status updates with notes |
| Email notifications | Confirmation, status updates, call-request alerts, cancellations |
| Multi-language UI | 11 South African languages supported |
| API documentation | Interactive Swagger UI with JWT auth support |

---

## Architecture Overview

The system is built around a **three-tier layered architecture** with a dedicated security boundary between the browser and the backend.

```
┌────────────────────────────────┐
│   Browser (React + TypeScript) │
└──────────────┬─────────────────┘
               │ HTTP (same-origin cookies)
               ▼
┌────────────────────────────────┐
│   BFF — Backend for Frontend   │  ← Node.js / Express (port 4000)
│   · HttpOnly cookie management │
│   · CSRF double-submit pattern │
│   · Rate limiting              │
│   · Request proxying           │
└──────────────┬─────────────────┘
               │ HTTPS + Bearer token (internal)
               ▼
┌────────────────────────────────┐
│   Backend API (ASP.NET Core)   │  ← .NET 10 (port 53839)
│   · Business logic             │
│   · JWT authentication         │
│   · Entity Framework Core      │
│   · Serilog structured logging │
└──────────────┬─────────────────┘
               │ EF Core
               ▼
┌────────────────────────────────┐
│   SQL Server                   │  ← Containerised (port 1433)
└────────────────────────────────┘
```

### Why a BFF (Backend for Frontend)?

A common security vulnerability in SPAs is storing JWTs in `localStorage` or JavaScript-accessible memory, making them vulnerable to XSS attacks. The BFF pattern solves this:

- The browser **never sees the JWT**. The BFF receives it from the backend on login and immediately stores it in an **HttpOnly cookie**, which JavaScript cannot read.
- All subsequent API calls from the browser go through the BFF, which attaches the token from its cookie before forwarding to the backend.
- The frontend communicates only with `localhost` (same origin), so no cross-origin credential sharing is required.
- **CSRF protection** is layered on top via the double-submit cookie pattern — the BFF issues a readable `csrf_token` cookie, and the frontend must echo it back as an `X-CSRF-Token` header on every state-changing request.

The BFF does **not** have its own Swagger UI. It is a thin, transparent proxy — every route it exposes maps 1:1 to a backend endpoint. The only BFF-specific endpoints are:

| Endpoint | Purpose |
|---|---|
| `GET /api/csrf-token` | Issues the CSRF cookie. Must be called before any POST/PUT/DELETE. |
| `POST /api/auth/logout` | Clears the HttpOnly auth and session cookies server-side. |
| `POST /api/auth/login` | Special-cased: strips the JWT from the response and stores it in a cookie before returning to the browser. |

For full API documentation, see the [backend Swagger UI](#api-documentation-swagger).

---

## Technology Stack & Design Decisions

### Backend — ASP.NET Core / C# (.NET 10)

**Why C# and ASP.NET Core?**

C# is a statically-typed, object-oriented language that enforces **strong type safety at compile time**, catching entire categories of bugs before they reach production. For a financial system handling transaction disputes, this matters enormously — a mismatch between a dispute DTO and the database entity is a compile error in C#, not a runtime crash.

ASP.NET Core was chosen because:
- It is **asynchronous by default**. Every I/O operation — database reads, email sends, HTTP calls to the translation API — uses `async/await`, meaning threads are never blocked waiting for a slow operation. This allows a single server to handle thousands of concurrent requests efficiently, critical when a banking platform may receive millions of transactions.
- It has **first-class dependency injection** built in, enabling clean separation between interfaces and implementations, and making unit testing straightforward — mock the interface, not the class.
- **ASP.NET Core Identity** provides a battle-tested, audited authentication system: user management, PBKDF2 password hashing, role management, and TOTP-based MFA with no extra cost.

**Why Object-Oriented Programming (OOP)?**

The domain model is naturally object-oriented. A `Dispute` is an object with state (its current status), behaviour (it transitions through states), and relationships (it belongs to a `User`, references a `Transaction`, and accumulates a history of `DisputeStatusHistory` entries). OOP's core principles serve this directly:

- **Encapsulation** — entities own their data. `DisputeService` enforces that only the owning customer can cancel their dispute. The rule lives in one place.
- **Abstraction** — `IEmailService`, `ITranslationService`, and `IDisputeService` are interfaces. Controllers and services depend on abstractions, not concrete implementations. This is what makes the system testable and swappable.
- **Single Responsibility** — each class does one thing. `AuthService` handles authentication. `DisputeService` handles disputes. `EmailService` sends emails. No class knows about more than it needs to.
- **Inheritance and polymorphism** — `User`, `Employee`, and `BaseEntity` share common fields through inheritance, avoiding duplication across the domain model.

**Why Clean Architecture (4-layer separation)?**

```
Capitec.Dispute.Domain         ← Core entities, no dependencies on any other layer
Capitec.Dispute.Application    ← Business logic, interfaces, DTOs, validators
Capitec.Dispute.Infrastructure ← EF Core, email, external APIs (translation)
Capitec.Dispute.API            ← HTTP entry point, controllers, middleware, Swagger
```

Each layer depends only on the layer below it — never above. This means:
- **Business logic is framework-agnostic.** The `DisputeService` does not know or care whether it is being called from an HTTP controller or a background job.
- **Infrastructure can be swapped.** The `IEmailService` interface is defined in `Application`. The SMTP implementation lives in `Infrastructure`. Replacing it with AWS SES requires changing one line in `Program.cs` — nothing else.
- **Testing is clean.** Controller tests inject mock services. Service tests inject mock repositories. No concrete class is tightly coupled to any other.

**Key backend packages:**

| Package | Purpose |
|---|---|
| `Entity Framework Core 10` | ORM — code-first migrations, LINQ queries, retry-on-failure |
| `ASP.NET Core Identity` | User management, PBKDF2 hashing, role management, TOTP MFA |
| `FluentValidation` | Declarative, reusable validation rules decoupled from controllers |
| `Serilog` | Structured logging with rolling file sinks and JSON output |
| `Swashbuckle.AspNetCore 6.9` | Interactive Swagger / OpenAPI documentation UI |
| `Polly` | Resilience policies (retry, circuit breaker for external services) |
| `AutoMapper` | Clean mapping between domain entities and DTOs |

---

### Frontend — React 18 / TypeScript / Vite

**Why React?**

React's **component model** maps naturally to a UI with repeated patterns — a dispute card, a status badge, a modal. Each component owns its own state and re-renders only when that state changes, making the UI efficient and predictable. React's **unidirectional data flow** means data always moves downward from parent to child, and events move upward via callbacks. This makes it easy to reason about where state lives at any given moment — important when displaying financial data where incorrect information has real consequences.

**Why TypeScript over plain JavaScript?**

TypeScript adds a static type system to JavaScript. In this project, every API response shape is typed as an interface. If the backend changes a field name and the frontend interface is not updated, TypeScript catches it at build time rather than as a silent `undefined` bug at runtime. For a project where incorrect data could cause a customer to dispute the wrong transaction, this safety net is essential.

**Why Vite?**

Vite uses **native ES modules** during development, meaning the browser only loads the specific module that changed rather than re-bundling the whole application. Hot module replacement (HMR) is near-instant. For a large multi-page application, this difference in developer experience is significant.

**Why Tailwind CSS?**

Traditional CSS approaches (separate stylesheets, BEM, CSS Modules) require context-switching between a component and its styles. Tailwind's utility-first approach keeps styles co-located with the markup — you can read a component and understand exactly how it looks without opening another file. Tailwind also produces tiny production bundles by stripping every class that is not used in the project.

**Key frontend packages:**

| Package | Purpose |
|---|---|
| `React 18` | UI component framework |
| `React Router 6` | Client-side routing (SPA navigation without page reloads) |
| `Axios` | HTTP client with interceptors (automatic CSRF header, 401 redirect) |
| `TypeScript 5` | Static typing and compile-time safety |
| `Tailwind CSS 3` | Utility-first styling aligned to Capitec brand colours |
| `Vitest + Testing Library` | Unit and component tests |

---

### BFF — Node.js / Express / TypeScript

**Why Node.js for the BFF?**

The BFF is a **thin proxy** — it adds security headers, manages cookies, validates CSRF tokens, applies rate limiting, and forwards requests. It has no database queries or heavy computation. Node.js is well-suited because its **event loop** handles thousands of concurrent proxy connections without spawning threads. Using TypeScript maintains the same type discipline as the frontend.

---

### Database — SQL Server

SQL Server was chosen for its:
- **ACID transaction support** — disputes involve multiple related records (the dispute itself, status history, notifications). These must all succeed or all fail together.
- **EF Core code-first migrations** — the database schema evolves alongside the code, is version-controlled in git, and is applied automatically on startup with zero manual SQL scripts.
- **Connection resilience** — the EF Core `EnableRetryOnFailure` policy handles transient SQL Server connection failures automatically, essential in a containerised environment where the database may not be immediately ready.

---

## Project Structure

```
Transaction Dispute Portal/
├── backend/                              # ASP.NET Core solution
│   ├── Capitec.Dispute.Domain/           # Entities: User, Dispute, Transaction, Employee…
│   ├── Capitec.Dispute.Application/      # Interfaces, DTOs, validators, business logic
│   ├── Capitec.Dispute.Infrastructure/   # EF Core, email, translation, activity logging
│   ├── Capitec.Dispute.API/              # Controllers, middleware, Program.cs, Swagger
│   ├── Capitec.Dispute.API.Tests/        # Controller-level unit tests (xUnit)
│   ├── Capitec.Dispute.Application.Tests/
│   ├── Capitec.Dispute.Infrastructure.Tests/
│   └── Dockerfile
├── frontend/                             # React + TypeScript SPA
│   ├── src/
│   │   ├── pages/                        # Full-page route components
│   │   │   └── employee/                 # Employee-specific pages
│   │   ├── services/api.ts               # Axios client with CSRF + 401 interceptors
│   │   ├── context/                      # React context (language)
│   │   └── utils/translations.ts        # 11 South African language strings
│   └── Dockerfile
├── bff/                                  # Backend for Frontend (Express proxy)
│   └── src/index.ts                      # All BFF routes, CSRF, cookies, rate limiting
├── docker-compose.yml                    # Orchestrates SQL Server, backend, frontend
└── README.md
```

---

## Before You Start — SMTP Setup

> **Please read this before running the project.** Email configuration is required to experience the full application. Setting it up takes approximately 2 minutes.

### Why SMTP matters

| Feature | Without SMTP | With SMTP |
|---|---|---|
| Employee portal (seeded accounts) | Works | Works |
| Customer registration | Blocked — verification code is emailed | Works |
| Customer login (after registration) | Blocked | Works |
| Forgot password | Blocked — reset code is emailed | Works |
| Dispute confirmation email | Skipped | Sent |
| Status update email | Skipped | Sent |
| Cancellation email | Skipped | Sent |

If you only want to explore the **employee portal**, you can use the seeded accounts listed in the [Employee Portal](#employee-portal) section and skip this step. To experience the **full customer journey**, SMTP must be configured.

### Setting up a Gmail App Password (recommended, ~2 minutes)

1. Sign in to the Google Account you want to use for sending emails.
2. Go to **Security → 2-Step Verification** and enable it if not already on (required before App Passwords are available).
3. Go to **Security → 2-Step Verification → App passwords** (scroll to the bottom of the 2-Step Verification page).
4. Click **Create**, give it a name (e.g. "Dispute Portal"), and click **Create**.
5. Copy the 16-character password shown — you will not be able to see it again.

### Applying the credentials

**Local development (`dotnet run`):**

Open `backend/Capitec.Dispute.API/appsettings.Development.json` and fill in your details:

```json
"Smtp": {
  "Host": "smtp.gmail.com",
  "Port": "587",
  "Username": "your-gmail@gmail.com",
  "Password": "xxxx xxxx xxxx xxxx",
  "FromEmail": "your-gmail@gmail.com",
  "FromName": "Capitec Dispute Portal"
}
```

**Docker:**

Open the `.env` file at the project root and set:

```env
SMTP_USERNAME=your-gmail@gmail.com
SMTP_PASSWORD=xxxx xxxx xxxx xxxx
```

> If SMTP is intentionally left unconfigured, all email operations are silently skipped with a warning log entry. No errors are thrown and the application continues to run — email is treated as a non-critical notification channel.

---

## Prerequisites

| Tool | Version | Purpose |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0+ | Backend build and run |
| [Node.js](https://nodejs.org/) | 18+ | Frontend and BFF |
| [SQL Server](https://www.microsoft.com/en-us/sql-server) | 2019+ | Database (or use Docker) |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Latest | Container orchestration |
| [dotnet-ef CLI](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) | 10.0+ | Running migrations manually |

Install the EF CLI tool globally if not already installed:
```bash
dotnet tool install --global dotnet-ef
```

---

## Running the Project

### Option A: Local Development

Runs each service individually with hot-reload. Best for active development.

#### 1. Start SQL Server

Use a local instance, or spin up only the database container:
```bash
docker-compose up sqlserver -d
```

#### 2. Configure the backend

Open `backend/Capitec.Dispute.API/appsettings.Development.json` and verify:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=TransactionDisputePortal;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
  },
  "Jwt": {
    "SecretKey": "your-development-secret-key-at-least-32-chars",
    "TokenExpirationMinutes": "60",
    "Issuer": "Capitec",
    "Audience": "DisputePortal"
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": "587",
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "your-email@gmail.com",
    "FromName": "Capitec Dispute Portal"
  },
  "BffOrigin": "http://localhost:4000"
}
```

> **SMTP is optional.** If `Smtp.Host` is not configured, the application logs a warning and skips sending emails — all other functionality works normally.

#### 3. Run the backend

```bash
cd backend
dotnet run --project Capitec.Dispute.API
```

Database migrations are applied automatically on startup. The API will be available at:
- HTTPS: `https://localhost:53839`
- HTTP: `http://localhost:53840`
- Swagger UI: `https://localhost:53839/swagger`

#### 4. Configure and run the BFF

```bash
cd bff
```

Create a `.env` file:
```env
BFF_PORT=4000
BACKEND_URL=https://localhost:53839
FRONTEND_ORIGIN=http://localhost:3000
NODE_ENV=development
```

```bash
npm install
npm run dev
```

The BFF will be available at `http://localhost:4000`.

#### 5. Configure and run the frontend

```bash
cd frontend
```

Create a `.env` file:
```env
VITE_API_BASE_URL=http://localhost:4000/api
```

```bash
npm install
npm run dev
```

The frontend will be available at `http://localhost:3000`.

---

### Option B: Docker (Full Stack)

Builds and runs the entire stack in containers. Best for a clean, reproducible demo.

#### 1. Configure secrets

```bash
cp .env.example .env
```

Edit `.env` and set a strong `DB_PASSWORD`. The password must meet SQL Server complexity requirements (min 8 characters, mix of uppercase, lowercase, digits, and symbols).

#### 2. Start the stack

```bash
docker-compose up --build
```

| Service  | URL |
|---|---|
| Frontend | `http://localhost:3000` |
| BFF      | `http://localhost:4000` |
| SQL Server | `localhost:1433` |

> The backend has no published port in normal mode — it is only reachable internally through the BFF. This prevents the API from being accessed directly, bypassing cookie and CSRF protection.

#### 3. Start with Swagger (development/debug)

To expose the backend and enable Swagger, use the debug override:

```bash
docker-compose -f docker-compose.yml -f docker-compose.debug.yml up --build
```

| Service        | URL |
|---|---|
| Frontend       | `http://localhost:3000` |
| Backend Swagger | `http://localhost:8080/swagger` |

Stop all containers:
```bash
docker-compose down
```

Stop and remove all data including the database volume:
```bash
docker-compose down -v
```

---

## Configuration & Secrets

**Never commit secrets to source control.** A `.env` file at the project root supplies secrets to Docker Compose — it is git-ignored. Copy `.env.example` to get started:

```bash
cp .env.example .env
```

For local development outside Docker, supply sensitive values via a local-only `appsettings.*.json` file that is excluded from `.gitignore`.

| Setting | Description |
|---|---|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `Jwt__SecretKey` | HS256 signing key — minimum 32 characters, use a cryptographically random value in production |
| `Smtp__Host` / `Smtp__Username` / `Smtp__Password` | Email provider credentials |
| `BffOrigin` | The BFF's origin URL — backend CORS is locked to this value only |

In production, the application will **throw on startup** if the JWT secret is missing, too short, or still set to the default development value. This prevents accidental deployment with weak credentials.

---

## Using the Application

### Customer Portal

Access at: `http://localhost:3000`

#### Registration

1. Click **Sign up** on the login page.
2. Please ensure that you use a real email address on step below as emails will be sent to this address for verification codes as well as dispute updates.
3. Enter your first name, last name, email address, and phone number, then click **Send Verification Code**.
4. A 6-digit code will be emailed to you. Enter it on the verification screen.
5. Set a password (minimum 8 characters, must include at least one uppercase letter, one digit, and one special character).
6. Submit — you will be redirected to the login page.

#### Login

1. Enter your email and password.
2. You will be taken directly to the dashboard.

#### Dashboard

After login you will see:
- Your name, email address, and account number.
- **Dispute summary stats** — live counts of open, resolved, rejected, and cancelled disputes.
- **Recent transactions** — the last 5 transactions on your account.
- **Open disputes** — your 3 most recent active disputes with their reference number, reason, and status.

#### Viewing Transactions

Navigate to **Transactions** in the top navigation bar. Transactions are paginated.

To generate test transactions, click **Simulate Transactions** — this creates 21 sample transactions on your account so you can explore the dispute flow without needing real data.

To raise a dispute on a transaction, click the **Dispute** button on any transaction row.

#### Raising a Dispute

**Step 1 — Select a reason:**
- Unauthorised — you did not authorise this transaction.
- Incorrect Amount — the amount charged is wrong.
- Double Payment — you were charged twice for the same transaction.
- Other — enter a custom reason.

**Step 2 — Write a summary:**

Describe the dispute in your own words (minimum 10 characters, maximum 500). You may write in any language — the system automatically detects the language, translates the summary to English, and stores both versions. The employee sees the English translation alongside a label showing the original language (e.g. "Translated: Afrikaans").

After submitting, you will receive a confirmation email containing your unique **incident reference number** (e.g. `INC-2026-0042`). Keep this number to follow up on your dispute.

#### Tracking Disputes

Navigate to **Disputes** in the top navigation bar.

- **Active tab** — disputes currently being processed (Submitted, Pending, Under Review).
- **Historical tab** — closed disputes (Resolved, Rejected, Cancelled). A badge appears when new disputes have been moved here since your last visit.
- Use the **Sort**, **Date**, and **Status** filters to narrow the list.
- Click **⋮ → View Details** on any dispute to see the full status history, including timestamps, employee names, and any notes left by the employee explaining their decision.
- Click **⋮ → Cancel** to withdraw a dispute you no longer wish to pursue. A reason is required.

#### Profile & Password

Navigate to **Profile** via the icon in the top-right of the navigation bar.

- Update your name or phone number — a verification code is sent to your email to confirm the change before it is applied.
- Change your password — your current password is verified first, then a confirmation code is emailed.

#### Forgot Password

On the login page, click **Forgot password?**

1. Enter your email address — a 6-digit reset code is sent.
2. Enter the code (valid for 10 minutes).
3. Enter and confirm your new password.
4. You are redirected to the login page.

#### Session Expiry

JWT sessions last 1 hour. If your session expires while you are using the portal, the next action will redirect you to the login page with the message: **"Your session has expired. Please log in again."**

---

### Employee Portal

Access at: `http://localhost:3000/employee/login`

#### Default Test Accounts

Two employee accounts are automatically created when the application starts for the first time. No registration or email verification is required — you can log in immediately.

| Name | Email | Password | Employee Code |
|---|---|---|---|
| Jane Smith | `employee1@capitec.co.za` | `Employee@123!` | `EMP-000001` |
| Johan DuToit | `employee2@capitec.co.za` | `Employee@456!` | `EMP-000002` |

> These accounts are seeded idempotently — if they already exist in the database (e.g. on a subsequent startup), they are not recreated or overwritten.

#### Registration

Additional employees can self-register at `/employee/register`. The flow is identical to customer registration with one addition — employees enter their **department** during sign-up. Each employee is automatically assigned a unique **employee code** (e.g. `EMP-482901`) which appears in the portal header and in audit logs.

#### Login

Employee login does not require MFA. Enter your email and password to proceed directly to the dashboard.

#### Employee Dashboard

The dashboard shows all customer disputes across the system, split into:

- **Active tab** — disputes currently in progress.
- **Historical tab** — resolved, rejected, or cancelled disputes.

**Summary stat cards** at the top show live counts per status: Pending, Under Review, Resolved, Rejected, Cancelled.

**Reference search** — type a customer's incident reference number (e.g. `INC-2026-0042`) in the search field to retrieve that specific dispute immediately, bypassing pagination.

#### Updating a Dispute Status

1. Click **Update Status** on any dispute row.
2. Select the new status:
   - **Pending** — acknowledged, queued for investigation.
   - **Under Review** — actively being investigated.
   - **Resolved** — dispute upheld in the customer's favour.
   - **Rejected** — dispute not upheld.
3. Optionally add **notes** to explain the decision. These are visible to the customer in their dispute detail view.
4. If selecting **Under Review**, you can tick **Request a call with the customer** — this adds a notice to the customer's status update email: *"A Capitec employee will call you within the next 15 minutes."*
5. Click **Update** — the customer receives an email notification immediately.

---

## API Documentation (Swagger)

The backend exposes an interactive Swagger UI with documentation for all endpoints.

### Accessing Swagger

**Local development (via `dotnet run`):**
```
https://localhost:53839/swagger
```

On first visit your browser will show a self-signed certificate warning. Click **Advanced → Proceed to localhost (unsafe)**. This only needs to be done once per browser session.

> Do **not** use the HTTP URL (`http://localhost:53840/swagger`). The `UseHttpsRedirection()` middleware redirects HTTP to HTTPS. This changes the origin of the request, which causes CORS to block all "Try it out" calls from Swagger.

**Docker (debug mode only):**

Swagger is disabled when `ASPNETCORE_ENVIRONMENT=Production`. To enable it in Docker, use the debug override:

```bash
docker-compose -f docker-compose.yml -f docker-compose.debug.yml up --build
```

Swagger will then be available at:
```
http://localhost:8080/swagger
```

### Getting a JWT Token

1. Expand `POST /api/employee/login` (or `POST /api/auth/login` for a customer token).
2. Click **Try it out**.
3. Enter valid credentials. You can use one of the seeded default employee accounts:
```json
{
  "email": "employee1@capitec.co.za",
  "password": "Employee@123!"
}
```
4. Click **Execute**.
5. Copy the `token` value from the response — the long string beginning with `eyJ…`

### Authorising Swagger

1. Click the **Authorize** button (padlock icon, top right of the Swagger page).
2. Paste **only the raw token** — do **not** add `Bearer` before it:
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```
3. Click **Authorize** → **Close**.

> **Common mistake:** Typing `Bearer eyJ...` manually. Swagger prepends `Bearer ` automatically. Doing this results in `Bearer Bearer eyJ...` in the Authorization header, which produces a 401 Unauthorized error on every request.

### Testing Endpoints

Protected endpoints show a closed padlock icon. Click **Try it out** → fill in any required parameters → **Execute** to make a live call against the running API.

---

## Security Features

| Feature | Implementation |
|---|---|
| Password hashing | PBKDF2 with SHA-256, 10,000 iterations (ASP.NET Core Identity default) |
| JWT authentication | HS256-signed tokens, 1-hour expiry, zero clock skew tolerance |
| MFA (planned) | TOTP (RFC 6238) backend infrastructure built via ASP.NET Core Identity — UI wiring is a planned enhancement |
| HttpOnly cookies | JWTs stored in HttpOnly cookies by the BFF — not accessible to JavaScript |
| CSRF protection | Double-submit cookie pattern — `csrf_token` cookie echoed back as `X-CSRF-Token` header |
| CORS | Backend locked to BFF origin only — all other origins are rejected |
| Rate limiting | Auth endpoints: 15 requests per 15 min (BFF) + 20 requests per 15 min (backend). Password reset: 10 requests per hour |
| Input validation | FluentValidation on all DTOs — returns structured 400 responses, never raw exceptions |
| Email enumeration prevention | `POST /api/auth/forgot-password` always returns 200 regardless of whether the email exists |
| Session expiry | 401 responses redirect to the login page with a clear expiry message |
| Secrets protection | Application refuses to start in production if the JWT secret is missing, too short, or uses the default development value |
| Global exception middleware | All unhandled exceptions are caught, logged, and returned as consistent JSON error responses — the server never crashes or leaks stack traces |

---

## Email Notifications

The application sends HTML-formatted emails for the following events:

| Trigger | Subject |
|---|---|
| Registration | Your Registration Verification Code |
| Dispute submitted | Dispute Submitted – Reference INC-XXXX |
| Dispute status updated | Dispute Status Update – Reference INC-XXXX |
| Call requested by employee | Included in the status update email with a blue call-request notice |
| Dispute cancelled by customer | Dispute Cancelled – Reference INC-XXXX |
| Password change request | Your Password Change Verification Code |
| Password reset request | Your Password Reset Code |
| Profile update request | Your Profile Update Verification Code |

> For setup instructions see [Before You Start — SMTP Setup](#before-you-start--smtp-setup).

---

## Testing

### Backend Tests

Tests use **xUnit** with **Moq** for mocking service dependencies. Controller tests cover all major endpoints: authentication, dispute management, transaction queries, user profile operations, and employee actions.

Run all backend tests:
```bash
cd backend
dotnet test
```

Run tests for a specific project:
```bash
dotnet test Capitec.Dispute.API.Tests
dotnet test Capitec.Dispute.Application.Tests
dotnet test Capitec.Dispute.Infrastructure.Tests
```

### Frontend Tests

Tests use **Vitest** and **React Testing Library**.

Run all frontend tests:
```bash
cd frontend
npm test
```

Run in watch mode (re-runs on file save):
```bash
npm run test:watch
```

Open the interactive visual test UI:
```bash
npm run test:ui
```

---

## Logging & Monitoring

The backend uses **Serilog** with structured logging. Log output goes to:

| Sink | Location | Contents |
|---|---|---|
| Console | Terminal output | All log levels during development |
| System log (text) | `logs/system/app-YYYYMMDD.log` | Infrastructure and error events, retained 30 days |
| System log (JSON) | `logs/system/app-json-YYYYMMDD.log` | Machine-readable structured logs for log aggregators |

Each log entry is enriched with machine name, thread ID, timestamp, and HTTP context (method, path, status code, response time in milliseconds).

### Health Check

```
GET /health
```

Returns a JSON summary of service health, including the SQL Server connection:

```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "sql",
      "status": "Healthy",
      "duration": "00:00:00.0123456"
    }
  ],
  "totalDuration": "00:00:00.0145678"
}
```

Returns `200 OK` when healthy, `503 Service Unavailable` when the database is unreachable.

---

## Future Enhancements

| Enhancement | Description |
|---|---|
| Multi-Factor Authentication | The backend TOTP infrastructure (secret generation, QR code provisioning, code verification via ASP.NET Core Identity) and the MFA setup UI page are fully built. The remaining work is wiring the setup page into the router and linking it from the customer Profile page, then extending the same flow to the employee portal. |
| File attachments | Allow customers to upload supporting evidence (e.g. receipts, screenshots) when submitting a dispute, stored in AWS S3. |
| Real-time notifications | WebSocket or Server-Sent Events integration to push live dispute status updates to the customer without requiring a page refresh. |
| Reporting & analytics | An admin dashboard showing dispute volumes, average resolution times, and status breakdowns — exportable as PDF or CSV. |
| E2E tests | Playwright end-to-end test suite covering the full customer and employee flows against a running stack. |
| SMS notifications | Replace the current development mock with a live SMS provider (e.g. Twilio or AWS SNS) so customers can opt in to receive status updates via SMS in addition to email. |

---

*Built with ASP.NET Core 10, React 18, TypeScript, Tailwind CSS, Entity Framework Core, and SQL Server.*

# ProcureFlow

ProcureFlow is a portfolio-grade, multi-branch purchase request management system built with
ASP.NET Core 9, React 19, TypeScript, EF Core and PostgreSQL. It is being developed from an
existing production-minded starter into a focused B2B product with organization-scoped access,
catalog management, purchase-request workflows, monthly budgets and approvals.

> [!IMPORTANT]
> The technical foundation is already operational, but the ProcureFlow business domain is under
> active development. `Projects` and `ProjectTasks` are a temporary demonstration domain retained
> until the replacing ProcureFlow workflow passes PF5 validation. They are not the target product
> contract. The canonical implementation order is the
> [ProcureFlow product roadmap](doc/ROADMAP/PROCUREFLOW/00_PRODUCT_ROADMAP_OVERVIEW.md).

## ProcureFlow v1.0 Scope

- one organization with multiple branches and branch-scoped business memberships
- product categories, units of measure and a small purchasing catalog
- purchase-request drafts, line items, submission and cancellation
- monthly branch budgets, manager approval and procurement escalation
- ordered and delivered fulfillment states
- decision history, notifications, attachments and a focused dashboard
- PostgreSQL-backed transaction, authorization and concurrency tests
- one critical browser workflow from request creation to delivery

## Technical Foundation Available Today

- Clean Architecture backend split into `API`, `Application`, `Domain`, `Infrastructure`, and `Shared`
- React + TypeScript frontend with protected routes, authenticated session handling, runtime feature gating, and a quick search shell
- JWT access tokens with secure refresh-token rotation in HttpOnly cookies
- Email confirmation and email-based 2FA during sign-in
- Role-aware authorization for admin-only endpoints and views
- React Hook Form + Zod for frontend form validation
- Serilog logging and centralized exception handling
- Dockerfiles for backend and frontend
- Docker Compose setup with PostgreSQL, Mailpit for local email delivery, backend healthcheck, and frontend proxy-ready API routing
- Unit, integration, and smoke/E2E test projects
- Swagger UI in development

## Current Stack

### Backend

- .NET 9
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL in Docker/runtime scenarios
- FluentValidation
- Serilog
- xUnit + Moq

### Frontend

- React 19
- TypeScript
- React Router
- React Hook Form
- Zod
- Testing Library + Vitest
- Vite build pipeline with Vitest-based frontend tests

## Project Structure

```text
.
├── backend/
│   ├── API/                  # Controllers, middleware, startup, auth configuration
│   ├── Application/          # DTOs, services, validators, interfaces
│   ├── Domain/               # Entities, enums, domain interfaces
│   ├── Infrastructure/       # DbContext, repositories, infrastructure services
│   ├── Shared/               # Shared responses, settings, helpers
│   ├── UnitTests/            # Focused unit tests
│   ├── IntegrationTests/     # Backend integration tests
│   └── E2ETests/             # Deployment smoke tests against a running app
├── frontend/
│   ├── public/
│   └── src/
│       ├── components/
│       ├── context/
│       ├── hooks/
│       ├── pages/
│       ├── services/
│       ├── tests/
│       ├── types/
│       └── utils/
├── docker/
└── doc/
```

## Environment Configuration

This repository uses two configuration entry points:

1. Root `.env` for Docker Compose and backend runtime values.
2. `frontend/.env.*` files for local frontend build-time values.

Start from the tracked examples:

```powershell
Copy-Item .env.example .env
Copy-Item frontend/.env.example frontend/.env.development.local
```

Rules:

- Backend secrets belong in root `.env`, CI/CD secrets, or hosting configuration.
- Frontend `VITE_*` values are public at build time. Never store secrets there.
- For Docker/nginx deployments, use `FRONTEND_REACT_APP_API_URL=/api` (mapped to `VITE_API_URL` during the image build).
- For local frontend to local backend development, use `VITE_API_URL=http://localhost:5000`.
- The Compose example is intentionally `Development` over HTTP; it uses `SameAsRequest` for the refresh cookie.
- Production requires a non-example `JWT_SECRET`, `Always` secure refresh cookies, a persistent Data Protection key ring, and explicitly trusted forwarded proxy networks.

## Runtime Feature Flags

The frontend consumes `GET /api/runtime-config` during startup.

Product-neutral flags include:

- `GlobalSearchEnabled`
- `DashboardOverviewEnabled`
- `AdminNavigationEnabled`
- `UserManagementNavigationEnabled`
- `EmailFeatureSectionsEnabled`
- `EmailDeliveryEnabled`
- `EmailTwoFactorEnabled`
- `EmailTwoFactorEnabledForNewUsers`

Temporary demo-domain flags include:

- `ProjectsEnabled`
- `ProjectArchiveEnabled`
- `ProjectTaskAssignmentEnabled`

The demo-domain flags remain available only while `Projects` and `ProjectTasks` are retained during
the incremental migration to ProcureFlow.

Use `RuntimeConfigProvider` and `useFeatureAvailability()` to read them from the frontend.

## Quick Start

### Prerequisites

- .NET 9 SDK
- Node.js 22.x
- Docker Desktop with Compose support for containerized runs

### Run with Docker

```powershell
git clone https://github.com/Karol-Malecki-dev/ProcureFlow.git
Set-Location ProcureFlow
Copy-Item .env.example .env
docker compose up --build
```

Default local endpoints:

- Frontend: http://localhost:3000
- API: http://localhost:5000
- Health: http://localhost:5000/health
- Swagger UI: http://localhost:5000/swagger
- Mail UI: http://localhost:8025

### Run Locally Without Docker

Backend:

```powershell
Set-Location backend/API
dotnet run
```

Frontend:

```powershell
Set-Location frontend
npm install
npm start
```

If you run the frontend locally against the local backend, make sure `frontend/.env.development.local` contains:

```text
VITE_API_URL=http://localhost:5000
```

The frontend shell now waits for both auth and runtime config before rendering protected UI. When `GlobalSearchEnabled` is on, `Ctrl+K` opens the quick search bar in the navbar.

The default Docker Compose configuration is intended for local development and smoke testing. The CD
workflow publishes immutable SHA-tagged images and can deploy them to the protected VPS staging
environment, where it runs public HTTPS browser smoke with the staging Mailpit profile. Production
promotion remains manual and requires the V5 release gate, including backup, restore and rollback
evidence.

The backend persists Data Protection keys in the `data-protection-keys` Compose volume.
Keep that volume when restarting the stack; removing it invalidates the key ring used to
protect authenticator secrets.

## Operations

The API exposes health endpoints for deployment probes and monitoring:

- `GET /health` reports the API and database health, excluding background workers.
- `GET /health/live` reports that the API process is alive.
- `GET /health/ready` verifies that the configured database accepts connections.
- `GET /health/workers` reports the latest processing state of background workers.
- `GET /health/storage`, `/health/malware-scanner`, and `/health/email` expose dedicated dependency
	and delivery health for monitoring.

Every response includes `X-Correlation-ID`. Clients can provide this header to trace a request through structured API logs; otherwise the API generates a request identifier.

## Testing

### Backend

```powershell
dotnet test backend/UnitTests/UnitTests.csproj
dotnet test backend/IntegrationTests/IntegrationTests.csproj
dotnet test backend/E2ETests/E2ETests.csproj
```

`PostgreSqlIntegrationTests` starts PostgreSQL through Testcontainers. Docker Desktop must therefore
be running and expose a correctly configured Docker Engine endpoint before the complete integration
suite is executed. The remaining integration tests use a controlled in-memory store.

The integration suite includes PostgreSQL Testcontainers coverage. It applies the real EF Core migrations to a temporary PostgreSQL container, so Docker Desktop must be running before executing it.

```powershell
docker info
dotnet test backend/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~PostgreSqlIntegrationTests"
```

Smoke tests in `E2ETests` are meant for a running application, for example after `docker compose up` or after deployment.

To build the containers, wait for the backend and frontend, run the full test solution, and clean up automatically:

```powershell
./scripts/Invoke-E2ETests.ps1
```

Optional smoke-test overrides:

```powershell
$env:SMOKE_API_URL="http://localhost:5000"
$env:SMOKE_FRONTEND_URL="http://localhost:3000"
dotnet test backend/E2ETests/E2ETests.csproj
```

### Frontend

```powershell
Set-Location frontend
npm install
npm run test:once
npm run build
```

## Authentication Overview

- `POST /api/auth/register` creates a user and sends an email confirmation link
- `POST /api/auth/confirm-email` confirms the address and activates the account
- `POST /api/auth/login` returns either JWT tokens or a 2FA email challenge when email 2FA is enabled
- `POST /api/auth/verify-2fa` verifies the email code and then returns the access token plus refresh-token cookie
- `POST /api/auth/resend-2fa` rotates the active sign-in code and sends a fresh email
- `POST /api/auth/refresh-token` rotates the refresh-token cookie and returns a fresh access token
- `POST /api/auth/logout` revokes the refresh token and clears the cookie
- `GET /api/auth/me` returns the authenticated user profile

The frontend stores only the access token client-side. The refresh token stays in an HttpOnly cookie and is not exposed to JavaScript.

For local Docker runs, transactional emails are delivered to Mailpit. Open http://localhost:8025 to see confirmation links and 2FA codes.

## Documentation

- [Documentation map](doc/README.md)
- [ProcureFlow product roadmap](doc/ROADMAP/PROCUREFLOW/00_PRODUCT_ROADMAP_OVERVIEW.md)
- [doc/GETTING_STARTED.md](doc/GETTING_STARTED.md)
- [doc/ARCHITECTURE.md](doc/ARCHITECTURE.md)
- [doc/BACKEND_SETUP.md](doc/BACKEND_SETUP.md)
- [doc/FRONTEND_SETUP.md](doc/FRONTEND_SETUP.md)
- [doc/JWT_ARCHITECTURE.md](doc/JWT_ARCHITECTURE.md)
- [doc/EMAIL_2FA_FLOWS.md](doc/EMAIL_2FA_FLOWS.md)
- [doc/CI_CD.md](doc/CI_CD.md)
- [docker/DOCKER_COMPOSE.md](docker/DOCKER_COMPOSE.md)
- [doc/ROADMAP/00_ROADMAP_OVERVIEW.md](doc/ROADMAP/00_ROADMAP_OVERVIEW.md)
- [backend/DEVELOPMENT_ROADMAP.md](backend/DEVELOPMENT_ROADMAP.md)

The `V1-V8` roadmap records the maturity of the inherited technical foundation. It does not define
the current product branch order.

## Current Product Step

PF0 is complete. The next implementation stage is
[PF1: organization, branches and access](doc/ROADMAP/PROCUREFLOW/02_PF1_ORGANIZATION_AND_ACCESS.md),
starting with `feature/organization-branches` and followed by `feature/branch-access-control`.

## License

MIT. See [LICENSE](LICENSE).
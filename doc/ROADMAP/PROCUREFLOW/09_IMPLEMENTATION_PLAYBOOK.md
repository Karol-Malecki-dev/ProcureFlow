# ProcureFlow Implementation Playbook

> [!IMPORTANT]
> This is the active execution guide for building ProcureFlow. Use it together
> with the detailed PF0-PF7 stage documents. The product is ProcureFlow; the
> existing technical foundation is an implementation resource, and `Projects`
> and `ProjectTasks` are temporary legacy modules. Do not extend the legacy
> domain when the work belongs to `Organizations`, `Catalog` or
> `PurchaseRequests`.

## Current position

- Product: `ProcureFlow`, a multi-branch purchase request management system.
- Completed: `PF0`, product scope and domain decisions.
- Start here: `feature/organization-branches`.
- Next branch: `feature/branch-access-control`.
- Release target: `ProcureFlow v1.0.0` after `PF1-PF6`.

## Start now

For the first implementation session, do only this:

1. Read [PF1: organization and access](02_PF1_ORGANIZATION_AND_ACCESS.md).
2. Implement `Organization`, `Branch` and the `CreateBranch` slice.
3. Add `ListBranches` and `GetBranchDetails` only after creation and its tests
  are working.
4. Finish the branch with archive behavior, PostgreSQL constraint coverage and
  the `OrganizationBranches` screen.
5. Do not add memberships, `BusinessRole` or catalog code until this branch
  satisfies the Definition of Done.

The first useful proof is simple: an administrator can create and list a branch,
while a duplicate branch code or name is rejected by the database.

The implementation order is deliberately incremental. Every branch must leave a
working state that can be built, tested and explained. Do not open the next
branch only because the previous branch contains code; open it after the previous
branch meets its Definition of Done.

## 1. Product context

ProcureFlow has three different documentation layers:

| Layer | Meaning | How to use it |
|---|---|---|
| Product roadmap | PF0-PF7 and the business scope | Decides what to build next. |
| Current implementation | Code and tests in the worktree | Decides what already exists. |
| Technical history | V1-V8, old ADRs and dated audits | Explains how the foundation matured; it is not a product backlog. |

The current code already provides authentication, PostgreSQL persistence,
notifications, storage, observability, Docker and automated tests. That does not
mean that the ProcureFlow domain is complete. During PF1-PF5, the legacy modules
remain available for regression coverage. PF6 removes them only after the new
critical workflow has replaced their user-facing responsibility.

Do not rename JWT issuer and audience values, Data Protection application names,
Docker containers, volumes, image paths or deployment directories as part of a
feature branch. Those are compatibility identifiers and require the separate PF6
migration decision.

## 2. The implementation loop

Use this sequence for every feature branch. The detailed stage document gives the
business rules; this loop tells you where to put the work.

### Step 1: Define the slice before writing code

Write a short note in the branch description or issue:

- user and business problem;
- actor and resource scope;
- successful result;
- invalid states and expected HTTP status codes;
- data that must be written atomically;
- test that would prove the most important invariant.

Do not begin with a controller, database table or React form. Begin with the
business rule and the boundary that owns it.

### Step 2: Model the domain

Add or change an entity, value object or enum only when it represents a product
concept. Keep invariants that depend only on the aggregate state inside the
aggregate. Do not accept client-provided totals, statuses, prices or access
claims as trusted values.

Typical locations:

```text
backend/Domain/Entities/<Entity>.cs
backend/Domain/ValueObjects/<ValueObject>.cs
backend/Domain/Enums/<Enum>.cs
```

Before continuing, write unit tests for the important state transitions. A domain
unit test should not need ASP.NET, EF Core or a real database.

### Step 3: Add persistence deliberately

When the feature needs storage:

1. add the `DbSet` and EF configuration;
2. define required foreign keys, unique constraints, precision and indexes;
3. create an EF Core migration;
4. decide whether writes need one transaction or concurrency protection;
5. add a PostgreSQL test when the behavior depends on a relational constraint,
   transaction or race condition.

Do not create a generic repository only to hide `ApplicationDbContext`. The port
should describe the exact query or command required by the use case.

In this repository, the first persistence files are normally
`backend/Infrastructure/Data/ApplicationDbContext.cs` and a new configuration
file under `backend/Infrastructure/Data/Configurations/`. A new module entry
point belongs under `backend/Infrastructure/Modules/<BusinessModule>/` and is
registered from `backend/API/Services/AddProjectServices.cs`, following the
existing module registration pattern.

### Step 4: Build one vertical slice at a time

For a new ProcureFlow module use this structure:

```text
backend/Application/Modules/<BusinessModule>/<UseCase>/
backend/API/Modules/<BusinessModule>/<UseCase>/
backend/Infrastructure/Modules/<BusinessModule>/<UseCase>/
backend/UnitTests/Modules/<BusinessModule>/<UseCase>/
```

Depending on the use case, the slice contains:

```text
Application:
  <UseCase>Command.cs or <UseCase>Query.cs
  <UseCase>Handler.cs
  I<UseCase>Store.cs or another focused port

API:
  <UseCase>Controller.cs
  <UseCase>Request.cs
  <UseCase>Response.cs
  <UseCase>Validator.cs

Infrastructure:
  Ef<UseCase>Store.cs

UnitTests:
  <UseCase>HandlerTests.cs
```

The handler coordinates the use case. The controller translates HTTP. The EF
adapter implements the application port. The module entry point registers the
slice. No controller or handler should depend directly on the whole
`ApplicationDbContext` when a focused port is sufficient.

### Step 5: Add the HTTP contract

For every endpoint document:

- HTTP method and route;
- request body and validation rules;
- response body;
- `401`, `403`, `404`, `409` and validation behavior where applicable;
- authorization scope;
- whether the operation is idempotent;
- whether the operation writes history or notifications.

Use request and response types owned by the new business module. Do not copy
`ProjectTask` contracts into a new folder under a different name.

### Step 6: Add the frontend after the API contract is stable

Use the existing shell and HTTP client, but create new ProcureFlow types and API
clients:

```text
frontend/src/types/<module>.ts
frontend/src/services/api/<Module>Api.ts
frontend/src/pages/<Page>.tsx
frontend/src/tests/pages/<Page>.test.tsx
```

The frontend may hide an unavailable action, but the backend remains responsible
for authorization. Every new page must handle loading, empty, error and success
states. A `409 Conflict` should give the user a way to refresh stale data.

### Step 7: Update documentation and validate

Before closing the branch:

- update the relevant PF stage document if scope or status changed;
- document a new domain decision in an ADR only when it is genuinely reusable;
- run focused unit and API tests;
- run the frontend test and build when UI or contracts changed;
- run the complete build and broader tests before marking the stage complete.

## 3. PF1 in detail: organization and access

PF1 is the first implementation stage. It creates the resource context required
by the catalog and purchase requests. Implement its two branches in order.

### Branch A: `feature/organization-branches`

**Goal:** create and manage branches inside the single MVP organization.

Do not add memberships or business roles in this branch. Keeping that boundary
small makes ownership and authorization easier to test.

#### Domain work

Create the following concepts, adjusting names only if the existing code reveals
a stronger local convention:

```text
backend/Domain/Entities/Organization.cs
backend/Domain/Entities/Branch.cs
backend/Domain/ValueObjects/BranchCode.cs       # only if a value object adds real invariants
```

Required rules:

- an organization has one or more branches;
- a branch belongs to exactly one organization;
- branch name and code are normalized before comparison;
- branch code or name is unique within the organization;
- an archived branch cannot receive new business data;
- a branch used by historical data is archived, not hard-deleted.

Write unit tests for branch creation, normalization, duplicate values at the
domain boundary and archiving rules.

#### Application and API work

Add focused slices under the `Organizations` module:

```text
Application/Modules/Organizations/CreateBranch/
Application/Modules/Organizations/ListBranches/
Application/Modules/Organizations/GetBranchDetails/
Application/Modules/Organizations/UpdateBranch/
Application/Modules/Organizations/ArchiveBranch/

API/Modules/Organizations/CreateBranch/
API/Modules/Organizations/ListBranches/
API/Modules/Organizations/GetBranchDetails/
API/Modules/Organizations/UpdateBranch/
API/Modules/Organizations/ArchiveBranch/

Infrastructure/Modules/Organizations/CreateBranch/
Infrastructure/Modules/Organizations/ListBranches/
Infrastructure/Modules/Organizations/GetBranchDetails/
Infrastructure/Modules/Organizations/UpdateBranch/
Infrastructure/Modules/Organizations/ArchiveBranch/
```

The first API contract may use these routes:

```text
GET    /api/organizations/{organizationId}/branches
POST   /api/organizations/{organizationId}/branches
GET    /api/organizations/{organizationId}/branches/{branchId}
PUT    /api/organizations/{organizationId}/branches/{branchId}
POST   /api/organizations/{organizationId}/branches/{branchId}/archive
```

The server must determine the organization scope from authenticated access. An
`organizationId` in the URL is an identifier to validate, not proof of access.

#### Persistence and integration tests

Add the organization and branch mapping, then test on PostgreSQL:

- duplicate branch code or name is rejected by the database;
- an archived branch cannot be used for a new business operation;
- a user cannot read or mutate another organization if the implementation allows
  more than one organization record in test data;
- an existing branch can be archived without deleting historical references;
- list filtering and ordering happen in the database.

The test file can begin as:

```text
backend/IntegrationTests/OrganizationsApiIntegrationTests.cs
```

If the scenario specifically proves a relational constraint or migration, add it
to the PostgreSQL integration coverage as well.

#### Frontend work

Add an administrative branch screen without replacing the entire legacy UI:

```text
frontend/src/types/organization.ts
frontend/src/services/api/OrganizationApi.ts
frontend/src/pages/OrganizationBranches.tsx
frontend/src/tests/pages/OrganizationBranches.test.tsx
```

The page needs loading, empty, error and archive states. The UI may show the
archive action only to an administrator, but the API must enforce that rule.

#### Suggested commit

```text
feat(organizations): add branch management
```

### Branch B: `feature/branch-access-control`

**Goal:** assign users to an organization and branch with a business role.

Add:

```text
backend/Domain/Entities/OrganizationMembership.cs
backend/Domain/Enums/BusinessRole.cs
backend/Application/Modules/Organizations/GetCurrentMembership/
backend/Application/Modules/Organizations/ListMemberships/
backend/Application/Modules/Organizations/AssignMembership/
backend/Application/Modules/Organizations/ChangeMembershipRole/
backend/Application/Modules/Organizations/RemoveMembership/
```

MVP rules:

- `Employee` and `Manager` require a `BranchId`;
- `Procurement` works at organization scope and does not require a branch;
- one user has at most one active membership in the MVP;
- an inactive user cannot be assigned;
- a manager can access only the assigned branch;
- a global technical `Admin` can manage memberships but is not automatically the
  business actor recorded in approval history.

Create one focused `ICurrentOrganizationContext` or equivalent port. Later
catalog and request handlers should use that context rather than repeating
membership queries.

Test at least:

- role and branch combinations;
- unique active membership;
- branch isolation;
- access loss after membership deactivation or user deactivation;
- negative API cases for `403 Forbidden`.

Suggested commit:

```text
feat(organizations): add branch memberships and business roles
```

## 4. Remaining implementation path

The following is the writing order after PF1. Each stage document contains the
complete business rules; this section gives the concrete implementation outcome.

### PF2: `feature/catalog-management`

**Write:** `Catalog` module with `ProductCategory`, `UnitOfMeasure` and
`Product`. Keep dictionary data in separate tables. Add active/archive behavior,
product search, filtering, pagination and explicit money precision.

**Suggested slices:**

```text
CreateProductCategory
CreateUnitOfMeasure
CreateProduct
UpdateProduct
ArchiveProduct
SetProductAvailability
ListCatalogProducts
GetCatalogProductDetails
```

**Tests:** unique codes, inactive references, non-negative prices, PostgreSQL
precision, database pagination and Employee versus Procurement authorization.

**Frontend:** catalog list, product form, category and unit selectors, archive
confirmation, loading/empty/error states.

**Suggested commit:**

```text
feat(catalog): add product catalog management
```

### PF3: purchase request core

Implement the aggregate before the workflow around it. `PurchaseRequestItem`
belongs to `PurchaseRequest` and is changed through aggregate methods.

#### Branch: `feature/purchase-request-drafts`

**Write:**

```text
backend/Domain/Entities/PurchaseRequest.cs
backend/Domain/Entities/PurchaseRequestItem.cs
backend/Domain/Enums/PurchaseRequestStatus.cs
backend/Application/Modules/PurchaseRequests/CreatePurchaseRequest/
backend/Application/Modules/PurchaseRequests/AddPurchaseRequestItem/
backend/Application/Modules/PurchaseRequests/UpdatePurchaseRequestItemQuantity/
backend/Application/Modules/PurchaseRequests/RemovePurchaseRequestItem/
backend/Application/Modules/PurchaseRequests/GetPurchaseRequestDetails/
backend/Application/Modules/PurchaseRequests/ListMyPurchaseRequests/
```

The aggregate owns draft editing, item uniqueness, positive quantity, price
snapshot, total calculation and `ConcurrencyStamp`. The client never supplies
the authoritative price or total.

**Tests:** aggregate transitions, quantity boundaries, snapshot behavior,
branch isolation, author-only editing and one successful update versus one
`409 Conflict` for a stale version.

**Frontend:** draft list, draft details and item editor using new
`PurchaseRequest` types and API clients.

Suggested commit:

```text
feat(purchase-requests): add request drafts and line items
```

#### Branch: `feature/purchase-request-submission`

**Write:** `SubmitPurchaseRequest`, `CancelPurchaseRequest`, status history and
validation that a request has at least one valid item. Submit must write the
status and first history event atomically.

**Tests:** submit, cancel, repeated invalid transitions, history persistence and
stale-version `409` behavior.

**Frontend:** submit and cancel actions, read-only submitted details, status
filter and stale-data refresh.

Suggested commit:

```text
feat(purchase-requests): add submission and cancellation workflow
```

### PF4: budgets and approvals

#### Branch: `feature/branch-monthly-budgets`

**Write:** `BranchMonthlyBudget` with branch, year, month, limit, reserved or
used amount and concurrency protection. Add Procurement/Admin management and
Manager read access for the assigned branch.

**Important test:** two concurrent operations must not reserve the same available
budget. Use PostgreSQL for the race condition; an in-memory provider is not enough.

Suggested commit:

```text
feat(budgets): add monthly branch budgets
```

#### Branch: `feature/purchase-request-approvals`

**Write:** Manager queue, approval in budget, escalation to Procurement,
Procurement approval or rejection, mandatory rejection reason, decision history
and atomic status-budget-decision writes.

**Rules to test:** no self-approval, no approval outside the branch, no repeated
decisions, correct `403` and `409` responses, and no partial budget update after a
failed transaction.

Suggested commit:

```text
feat(purchase-requests): add budget approval workflow
```

### PF5: product completeness

Implement in this order:

1. `feature/procurement-fulfillment`: `Approved -> Ordered -> Delivered`, actor
   and timestamp history, Procurement authorization.
2. `feature/purchase-request-attachments`: request-owned metadata with reusable
   storage, scanning and cleanup mechanics; do not copy `ProjectTaskAttachment`
   contracts mechanically.
3. `feature/purchase-request-notifications`: request events, neutral resource
   navigation, outbox and deduplication keys.
4. `feature/procureflow-dashboard`: SQL projections for pending requests, current
   month order value, popular products and branch spending.
5. `test/procureflow-critical-flow-e2e`: Employee creates and submits, Manager
   approves, Procurement orders and delivers, Employee sees final state.

Every PF5 branch needs negative authorization tests. The browser test is added
only after the API flow is stable; it should verify the business journey, not
repeat every API edge case.

Suggested commits:

```text
feat(purchase-requests): add procurement fulfillment
feat(purchase-requests): add request attachments
feat(notifications): add purchase request events
feat(reports): add procurement dashboard
test(e2e): cover ProcureFlow critical purchase flow
```

### PF6: cleanup and release

Do not start `refactor/remove-project-management-demo` until the PF5 critical
flow, PostgreSQL tests and browser E2E are green. Then remove the legacy domain in
small compilable steps:

1. disable old UI entry points;
2. remove frontend routes and API consumers;
3. remove API/module registrations;
4. remove Application contracts and handlers;
5. remove Infrastructure adapters;
6. remove EF mappings and add the intentional schema migration;
7. remove tests that cover only the retired domain;
8. search for stale symbols, routes and `ProjectId`-only contracts;
9. run the full build, tests, Compose smoke and staging gate;
10. update product documentation to describe only verified ProcureFlow scope.

Suggested commit:

```text
refactor: remove legacy project management domain
```

### PF7: post-MVP options

Do not write PF7 implementation code before `v1.0.0`. First choose one concrete
problem and create an ADR or feature brief. The first recommended option is CSV
spending export because it has clear value and low architectural cost.

## 5. Definition of Done for one branch

A branch is ready for review when all applicable points are true:

- the business rule is written in plain language;
- the owning aggregate or application boundary is clear;
- request and response contracts are explicit;
- authorization is enforced by the backend;
- persistence constraints and indexes are present;
- migrations are created when schema changes;
- focused unit tests cover domain rules;
- API or PostgreSQL tests cover the risky boundary;
- frontend states cover loading, empty, error and success when UI changes;
- documentation points to the actual implementation;
- the branch builds without new warnings;
- the commit describes one coherent feature.

A branch is not complete merely because the happy path works locally. Ask what
happens with a stale version, an unauthorized branch, a missing related record,
a duplicate request and a failed transaction.

## 6. Validation commands

Run the narrowest relevant checks while working:

```powershell
# Backend build
dotnet build backend/backend.slnx --configuration Release

# Backend unit tests
dotnet test backend/UnitTests/UnitTests.csproj --configuration Release

# Integration tests without Docker-dependent PostgreSQL coverage
dotnet test backend/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName!~PostgreSqlIntegrationTests"

# PostgreSQL tests; Docker Desktop must be running
dotnet test backend/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~PostgreSqlIntegrationTests"

# Frontend
Set-Location frontend
npm ci
npm run test:once
npm run build
```

Before PF6, run the complete repository gate:

```powershell
Set-Location ..
./scripts/Invoke-E2ETests.ps1
```

When a command cannot run because Docker is unavailable, record that limitation
explicitly. Do not mark PostgreSQL or Compose validation as passed based only on
unit tests.

## 7. Commit and branch template

Use one feature branch for one coherent user or technical outcome:

```text
<type>(<module>): <short outcome>

- describe the business capability or invariant
- describe authorization, transaction or concurrency behavior
- describe the tests that prove the change
```

Recommended types:

- `feat` for a new ProcureFlow capability;
- `fix` for correcting existing behavior;
- `test` for an independent test-only improvement;
- `docs` for product, architecture or roadmap documentation;
- `refactor` for a behavior-preserving cleanup such as PF6;
- `chore` for tooling or maintenance.

For the next branch, start with:

```text
Branch: feature/organization-branches
Goal: create and manage branches inside the MVP organization
First domain types: Organization, Branch
First slices: CreateBranch, ListBranches, GetBranchDetails
First integration proof: duplicate branch code is rejected by PostgreSQL
First frontend screen: OrganizationBranches
```

After completing that branch, write a short status update in the PF1 document:
what was implemented, which tests passed, what remains for
`feature/branch-access-control`, and which decision changed, if any.

# File plan: organization and branch management

- Status: Completed reference
- Purpose: show how one branch is split across layers

This is a planning map, not a command to create every path blindly. Before a
new change, compare the nearest existing implementation and keep the local
naming convention.

## 1. Layer map

```text
backend/
+-- Domain/
|   +-- Entities/
|   |   +-- Organization.cs
|   |   `-- Branch.cs
|   `-- ValueObjects/                 # only when a value object owns real rules
|
+-- Application/Modules/Organizations/
|   +-- CreateBranch/
|   +-- ListBranches/
|   +-- GetBranchDetails/
|   +-- UpdateBranch/
|   `-- ArchiveBranch/
|
+-- Infrastructure/Modules/Organizations/
|   +-- CreateBranch/
|   +-- ListBranches/
|   +-- GetBranchDetails/
|   +-- UpdateBranch/
|   `-- ArchiveBranch/
|
+-- API/Modules/Organizations/
|   +-- CreateBranch/
|   +-- ListBranches/
|   +-- GetBranchDetails/
|   +-- UpdateBranch/
|   `-- ArchiveBranch/
|
+-- IntegrationTests/
|   `-- OrganizationsApiIntegrationTests.cs
|
`-- UnitTests/
    `-- Organizations/                  # use the repository's real test layout
```

## 2. Responsibility map

```text
Domain
`-> owns valid Organization and Branch state

Application
`-> coordinates one use case and exposes focused ports

Infrastructure
`-> implements persistence and database queries

API
`-> binds HTTP, checks endpoint authorization and maps results

Tests
`-> prove behavior at the cheapest correct boundary
```

## 3. Writing order

1. Domain rule and unit test.
2. EF mapping, foreign keys, unique indexes and migration.
3. Application command/query, result and focused port.
4. Infrastructure store and handler implementation.
5. API request, response, validation and controller.
6. Integration/PostgreSQL tests.
7. Frontend types, API client and page.
8. Documentation and broader validation.

## 4. Important boundary

Do not put business rules in the controller or React. Do not make a generic
repository only to hide `ApplicationDbContext`. The store should describe the
query needed by the use case.

# File plan: branch access control

- Status: Implemented reference; PostgreSQL execution blocked locally
- Business operation: `AssignMembership`
- Technical operation: `CreateMembership`

This map describes responsibilities and likely locations. Before creating a
file, search for the nearest local convention and keep the existing naming.

## 1. Implemented structure

```text
backend/
+-- Domain/
|-- Domain/Models/Organizations/Membership.cs
|-- Domain/Models/Organizations/Enums/BusinessRole.cs
|-- Application/Modules/Organization/Membership/
|   +-- Create/
|   +-- Get/
|   +-- GetList/
|   +-- Update/
|   `-- Archive/
|-- Infrastructure/Modules/Organization/Membership/
|   +-- Create/
|   +-- Get/
|   +-- GetList/
|   +-- Update/
|   `-- Archive/
|-- API/Modules/Organization/Membership/
|   +-- Create/
|   +-- Current/
|   +-- List/
|   +-- Update/
|   `-- Archive/
|-- IntegrationTests/
|   +-- MembershipsApiInMemoryIntegrationTests.cs
|   `-- MembershipsApiIntegrationTests.cs
|-- UnitTests/
|   +-- Models/Organizations/MembershipTests.cs
|   `-- Modules/Organization/Membership/
|
`-- frontend/src/
    +-- pages/OrganizationMemberships.tsx
    +-- services/api/OrganizationApi.ts
    `-- tests/pages/OrganizationMemberships.test.tsx
|
`-- UnitTests/
    `-- Organizations/                     # follow the real test layout
```

## 2. Responsibility map

```text
OrganizationMembership entity
    -> owns valid role/branch combinations and membership state

Application handler
    -> coordinates active user, organization and branch checks

Focused membership store/port
    -> loads exactly the data required by the use case

PostgreSQL
    -> protects foreign keys, uniqueness and enforceable relational rules

API controller
    -> maps HTTP input, authorization and application results

Current membership port
    -> gives later business handlers one stable access context
```

## 3. Implementation and validation order

```text
1. Domain rule and unit tests
    -> 2. EF mapping, filtered unique index and migration
        -> 3. Application contracts and handlers
            -> 4. Infrastructure stores and DI registration
                -> 5. API contracts, authorization and current context
                    -> 6. API/InMemory/PostgreSQL tests
                        -> 7. Frontend client, page and tests
```

## 4. Boundary rules

- Do not put role/branch invariants only in the controller.
- Do not make the handler return `IActionResult`.
- Do not return HTTP statuses from a store.
- Do not trust `BranchId` from the request as authorization evidence.
- Do not query the whole database context from API code when a focused port is enough.
- Do not create a generic permission framework before a real requirement needs it.
- The route `organizationId` is the only organization scope source for write
    operations; it is not duplicated in the request body.
- PostgreSQL enforces one active membership per user globally, while inactive
    rows remain available as history.

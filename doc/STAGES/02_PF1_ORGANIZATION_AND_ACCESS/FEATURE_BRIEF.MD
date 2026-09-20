# PF1 stage brief

- Status: In progress
- Product: ProcureFlow
- Stage: `PF1`
- Canonical document: [`doc/ROADMAP/PROCUREFLOW/02_PF1_ORGANIZATION_AND_ACCESS.md`](../../ROADMAP/PROCUREFLOW/02_PF1_ORGANIZATION_AND_ACCESS.md)

## Stage goal

Create the organization context required by later catalog, purchase-request and
approval features. The backend must be able to answer two questions:

```text
Which organization and branch does this user belong to?
	-> Which operations can this user perform there?
```

## Branch boundary

```text
PF1
|
+-- 01 organization and branches
|   `-> create and manage the place where business data belongs
|
`-- 02 branch access control
	`-> connect users to organization, branch and business role
```

## Branch 01: completed

The first branch contains `Organization` and `Branch` administration:

- create, list, view, update and archive branches;
- organization-scoped name and code uniqueness;
- PostgreSQL constraints and historical references;
- administrator UI and API tests.

Detailed notes: [`01_organization-branches/`](01_organization-branches/README.md).

## Branch 02: next

The second branch contains:

- `OrganizationMembership`;
- `Employee`, `Manager` and `Procurement` business roles;
- one active membership per user in the MVP;
- branch and organization access context;
- authorization tests for isolation and access loss.

Detailed notes: [`02_branch-access-control/`](02_branch-access-control/README.md).

## Shared rules

- Global platform roles and business membership roles are separate concepts.
- A request identifier is not proof of authorization.
- Rules that PostgreSQL can enforce should also be protected in PostgreSQL.
- Historical data should not be destroyed only because current access changed.
- Catalog and purchase requests are outside PF1.

## Success

PF1 is complete when a later business handler can reliably resolve the current
organization/branch context and branch isolation is proven by integration tests.

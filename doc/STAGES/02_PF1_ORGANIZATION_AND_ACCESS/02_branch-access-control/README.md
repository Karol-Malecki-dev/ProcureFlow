# Branch 02: branch access control

- Status: Implemented; PostgreSQL execution blocked locally
- Branch: `feature/branch-access-control`
- Stage: `PF1`
- Source of truth: [`doc/ROADMAP/PROCUREFLOW/02_PF1_ORGANIZATION_AND_ACCESS.md`](../../../ROADMAP/PROCUREFLOW/02_PF1_ORGANIZATION_AND_ACCESS.md)

## Purpose

This branch connects users with an organization and, when required, a branch.
It introduces business roles, membership administration and the current access
context used later by the catalog and purchase-request modules.

## Simple model

```text
User
  |
  `-> OrganizationMembership
          +-> OrganizationId
          +-> UserId
          +-> BusinessRole
          +-> BranchId (optional)
          `-> IsActive / membership state

BusinessRole
  +-> Employee   -> requires BranchId
  +-> Manager    -> requires BranchId
  `-> Procurement -> organization scope, no BranchId required
```

## Main boundary

```text
Global platform role
    -> answers: can this technical user call the administration endpoint?

Business membership role
    -> answers: what can this user do inside this organization or branch?
```

Do not put `Employee`, `Manager` or `Procurement` into the global platform role
model unless the existing architecture proves that this boundary is wrong.

## Delivered scope

- Assign one active membership to an active user.
- List memberships in organization scope, with role, branch and active-state
  filters.
- Change the business role or branch while preserving the membership record.
- Archive a membership to remove current access without deleting history.
- Resolve the authenticated user's active membership through
  `GET /api/memberships/current`.
- Protect one active membership per user with the PostgreSQL filtered unique
  index `UX_Memberships_ActiveUser`.
- Provide the Admin membership-management page and its API client.

## Validation state

- Domain and backend unit tests: 371 passed.
- In-memory membership API tests: 4 passed.
- Module architecture tests: 3 passed.
- Frontend membership page tests: 5 passed.
- Frontend production build and typecheck: passed.
- PostgreSQL/Testcontainers tests, including the concurrent-assignment test,
  are implemented but blocked locally because Docker Engine is unavailable.

The remaining scope is future resource authorization for catalog and
purchase-request operations. Membership administration is intentionally kept
under the global technical `Admin` role; business roles remain organization or
branch scoped.

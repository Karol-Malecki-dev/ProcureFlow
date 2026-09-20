# Feature brief: branch access control

- Status: Implemented; PostgreSQL execution blocked locally
- Branch: `feature/branch-access-control`
- Stage: `PF1`
- First recommended slice: `AssignMembership`

## 1. Business problem

The system must know which organization and branch a user belongs to before it
can authorize catalog, purchase-request and approval operations.

## 2. Actors and resources

```text
Global technical Admin
    -> manages organization memberships

Authenticated business user
    -> receives an organization and branch access context

Resources
    -> User, Organization, Branch, OrganizationMembership
```

## 3. Successful result

A valid active user receives at most one active organization membership in the
MVP. The membership has a business role and, for branch-scoped roles, a valid
branch. Administrators can list, update and archive assignments, while later
handlers can ask one focused port for the current membership context instead of
repeating membership queries.

## 4. Invariants

- The user must be active.
- The organization must be active.
- The branch must belong to the organization.
- An archived branch cannot receive a new membership.
- `Employee` requires `BranchId`.
- `Manager` requires `BranchId`.
- `Procurement` works at organization scope and does not require `BranchId`.
- One user has at most one active membership in the MVP.
- Deactivation preserves the membership row for history and removes current
    access.
- Changing a branch does not rewrite historical purchase-request ownership.
- The client-provided branch identifier is not proof of access.
- Current access is checked from current database state, not trusted only from a token.

## 5. Expected failures

| Situation | Expected result |
|---|---|
| User is not authenticated | `401 Unauthorized` |
| Caller cannot manage memberships | `403 Forbidden` |
| User, organization or branch is outside the allowed scope | `404 Not Found` |
| Role and branch combination is invalid | Validation error |
| User is inactive | `404 Not Found` |
| Membership already exists, including a concurrent duplicate | `409 Conflict` |
| Organization is archived | `409 Conflict` |
| Branch is archived | `404 Not Found` |

The management endpoints reuse `[Authorize(Roles = "Admin")]`. Current
membership lookup is authenticated but does not require the Admin role.

## 6. Simplified flow

```text
Admin request
    -> API authorization
        -> AssignMembership handler
            -> load active User, Organization and Branch
                -> validate OrganizationMembership rule
                    -> PostgreSQL constraint and SaveChanges
                        -> response

Later business request
    -> current-user context port
        -> active membership
            -> branch/organization authorization
                -> business handler
```

## 7. Scope

Included:

- `OrganizationMembership`;
- `BusinessRole` with `Employee`, `Manager` and `Procurement`;
- assign, list, change and remove/deactivate membership;
- assign, list, update and archive membership;
- one active membership constraint;
- focused current-membership context port;
- backend and frontend membership-management tests;
- Admin membership-management page and API client.

Excluded:

- many organizations selected by one user;
- manager assigned to many branches;
- invitations;
- custom roles and permission editor;
- catalog and purchase-request business rules.

## Cheapest proving test

Start two assignments for the same active user at the same time. Exactly one
request must create the membership and the other must return `409 Conflict`.
The PostgreSQL test is implemented, but execution is currently blocked by the
unavailable local Docker Engine. A separate scope test rejects a branch from
another organization with `404 Not Found`.

Create a manager membership for branch A. Attempt to access a branch-scoped
resource in branch B. The request must be rejected even when the client sends
branch B in the route or request body.

# Test plan: branch access control

- Status: Implemented; PostgreSQL execution blocked locally
- Main risk: a technically authenticated user reaches a resource outside the
  organization or branch allowed by the current membership

## Behavior matrix

| Behavior | Test level | Proof |
|---|---|---|
| Valid role and branch combination is accepted | Domain unit | `Employee`/`Manager` require a branch; `Procurement` does not |
| Invalid role and branch combination is rejected | Domain unit | The entity cannot enter an invalid state |
| Inactive user cannot be assigned | Handler/API integration | The use case rejects the assignment |
| Archived branch cannot receive a membership | Handler/API integration | The use case checks current branch state |
| One active membership is enforced | PostgreSQL integration | A duplicate active membership cannot be persisted |
| Manager is isolated to the assigned branch | API/PostgreSQL integration | Branch B request is rejected for a manager assigned to A |
| Procurement has organization scope | API integration | Organization-level access works without `BranchId` |
| Deactivated membership loses access | API integration | Current database state overrides an old token |
| Current membership context is resolved once | API integration | `GET /api/memberships/current` reads the active database state |
| Unauthorized management is rejected | API integration | `401` and `403` cases are covered |
| UI does not become the security boundary | API integration | A manually sent forbidden request still fails |
| Two assignments race for the same user | PostgreSQL integration | Exactly one request succeeds and one maps the filtered-index conflict to `409` |

## Current validation

- Domain and backend unit tests pass: 371 tests.
- In-memory membership API tests pass: 4 tests.
- Module architecture tests pass: 3 tests.
- Frontend membership page tests pass: 5 tests.
- Frontend typecheck and production build pass.
- PostgreSQL/Testcontainers tests are implemented but cannot execute until the
  local Docker Engine is available.

## Cheapest proving test

Use one user, one organization and two branches:

```text
User U
  -> active Manager membership
      -> Branch A

Request for Branch A -> allowed
Request for Branch B -> rejected
```

This test proves the most important branch-isolation rule before the full UI is
built.

## Validation order

1. Domain and handler unit tests.
2. API authorization and InMemory membership tests.
3. Frontend page tests and production build.
4. PostgreSQL tests for uniqueness, foreign keys and concurrent assignment.
5. Release build and broader integration tests.

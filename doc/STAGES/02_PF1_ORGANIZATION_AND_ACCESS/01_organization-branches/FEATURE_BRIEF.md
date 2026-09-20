# Feature brief: organization and branch management

- Status: Completed
- Branch: `feature/organization-branches`
- Stage: `PF1`
- Scope: organization and administrative branch management

## 1. Business problem

The application needs a stable organization and branch context before it can
limit catalog items, purchase requests and approvals to the correct place.

## 2. Actor and resource

```text
Actor
`-> Administrator with permission to manage organization branches

Resource
`-> Branch belonging to one Organization
```

## 3. Successful result

An authorized administrator can create, view, update and archive a branch. The
branch remains available for historical data after archiving, but it cannot be
used for new business data.

## 4. Invariants

- A branch belongs to exactly one organization.
- A branch name and code are normalized before comparison.
- A branch name and code are unique inside one organization.
- An archived branch is not hard-deleted when history refers to it.
- An organization or branch identifier from a request is validated against access.
- The client cannot prove authorization by sending a different identifier.

## 5. Expected failures

| Situation | Expected result |
|---|---|
| User is not authenticated | `401 Unauthorized` |
| User cannot manage branches | `403 Forbidden` |
| Organization or branch does not exist in the allowed scope | `404 Not Found` |
| Name or code violates a uniqueness rule | `409 Conflict` or the repository's validation mapping |
| Input is invalid | The repository's standard validation response |

The exact status mapping must follow the existing API convention.

## 6. Simplified flow

```text
HTTP request
    -> API controller
        -> Application command/query
            -> Focused store
                -> EF Core
                    -> PostgreSQL

Domain rule
    -> validates the state of Organization or Branch

Application result
    -> API response and HTTP status
```

## 7. Scope

Included:

- `Organization` and `Branch` concepts;
- branch create, list, details, update and archive operations;
- normalization and organization-scoped uniqueness;
- PostgreSQL foreign keys, indexes and constraints;
- administrator branch screen with loading, empty, error and archive states.

Excluded:

- memberships;
- `Employee`, `Manager` and `Procurement` roles;
- resource authorization based on branch membership;
- catalog and purchase requests;
- multiple organization selection in the MVP.

## 8. Cheapest proving tests

1. A duplicate branch code or name is rejected inside one organization.
2. A request cannot read or change a branch from another organization.
3. An archived branch remains available for history but rejects new business use.

# Test plan: organization and branch management

- Status: Completed reference
- Main risk: a branch is accepted outside the correct organization or an
  invalid database state becomes possible

## Behavior matrix

| Behavior | Test level | Proof |
|---|---|---|
| Valid branch can be created | Domain + API | A valid request creates one branch |
| Name and code are normalized | Domain | Equivalent values are compared consistently |
| Duplicate name or code is rejected | PostgreSQL integration | The database protects organization-scoped uniqueness |
| Branch belongs to the requested organization | API integration | A branch from another organization cannot be read or changed |
| Archived branch is not deleted | PostgreSQL integration | Historical references remain valid |
| Archived branch cannot receive new data | Application/API integration | A new business operation is rejected |
| Unauthorized administrator action is rejected | API integration | The response is `401` or `403` according to the case |
| Branch page handles loading, empty and error | Frontend test | The user sees a stable state for each response |

## Test order

```text
Domain tests
    -> handler/application tests
        -> API integration tests
            -> PostgreSQL constraint tests
                -> frontend tests
                    -> full build and broader validation
```

## Cheapest proving test

Create two organizations and one branch in each. Send a request using the
identifier of organization A while targeting the branch from organization B.
The operation must be rejected.

## Completion rule

Do not mark the branch complete because the happy path works. The branch is
complete only when the important authorization, archival and PostgreSQL rules
are proved at the correct test level.

# Open questions: branch access control

- Status: Resolved for the MVP; future policy changes remain out of scope
- Rule: resolve a question before the slice that depends on it; record the
  decision in the roadmap or an ADR when it is reusable

| ID | Question | Why it matters | Recommended default | Status |
|---|---|---|---|---|
| BAC-001 | Is repeated assignment of the same active membership a conflict or an idempotent success? | Defines the command contract and `409` behavior | Return `409 Conflict`; the filtered unique index also protects concurrent requests | Resolved |
| BAC-002 | Does `RemoveMembership` mean hard delete or deactivation? | Affects history, access loss and foreign keys | Archive/deactivate the row, preserve history and remove current access | Resolved |
| BAC-003 | What exact policy allows a global Admin to manage memberships? | Prevents accidental reliance on a UI role | Reuse `[Authorize(Roles = "Admin")]` on management endpoints | Resolved |
| BAC-004 | Should an inaccessible resource return `403` or a scope-safe `404`? | Affects security and public API consistency | Return `404 Not Found` for missing, inactive or wrong-scope organization resources | Resolved |
| BAC-005 | Is the current membership context loaded from the database on every request? | Defines stale-access behavior | Resolve active state from the database through `GET /api/memberships/current` and its focused handler/store | Resolved |
| BAC-006 | What happens when a user's current branch is archived? | Defines access loss and migration behavior | Keep the membership for history; branch-scoped operations require an active branch | Resolved |

## How to resolve a question

```text
Question
    -> inspect existing code and tests
        -> compare one or two realistic options
            -> choose the smallest coherent rule
                -> update this file
                    -> update the relevant brief, test plan or ADR
```

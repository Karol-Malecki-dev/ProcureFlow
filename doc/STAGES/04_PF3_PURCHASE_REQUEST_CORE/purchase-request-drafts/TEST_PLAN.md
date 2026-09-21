# Test plan: purchase-request drafts

- Status: Planned
- Branch: `feature/purchase-request-drafts`
- Main risk: an authenticated user can read or mutate a request outside the
  organization/branch and the server trusts client-supplied price or total

## Cheapest proving test

Use one active organization, two active branches, two Employee memberships and
one selectable product:

```text
Employee A -> Branch A
Employee B -> Branch B
Product P  -> active and available in the organization

A creates a draft                         -> 201, BranchId is derived as A
A adds P with quantity 2                  -> 200, snapshot and total are server-owned
B reads A's draft                         -> safe 404 / no data leak
A updates with the old stamp after a write -> 409, newer data remains intact
```

This short scenario proves the most important PF3 boundary before the complete
frontend exists: branch isolation, author ownership, product snapshotting and
optimistic concurrency.

## Behavior matrix

| Behavior | Test level | Expected proof |
| --- | --- | --- |
| Employee creates an empty draft | Handler/API integration | Request is `Draft`, scoped to current organization and branch, with zero total |
| Manager or Procurement cannot create a draft in the Employee flow | API/handler | The operation is rejected by business authorization |
| Branch comes from active membership | Handler/API integration | Client-supplied branch data is absent or ignored; stored branch matches membership |
| Inactive membership cannot create or mutate | Handler/API integration | Current database state overrides stale client assumptions |
| Request author can read own draft | API integration | Details return only the author's scoped resource |
| Another user cannot read or mutate the draft | API integration | Safe `404` or documented forbidden result; no body leak |
| Archived/inactive branch cannot be used as current scope | Handler/API/PostgreSQL | Create and mutation are rejected |
| Add active and available product | Handler/API integration | Name, code, unit and price are copied into the item snapshot |
| Archived or unavailable product cannot be added | Handler/API integration | No item is written |
| Client price and total are ignored/rejected | API integration | Persisted values come from the catalog and aggregate |
| Duplicate product line is rejected | Domain/PostgreSQL | One product occurs at most once per request |
| Quantity is positive and within the documented limit | Domain/API/PostgreSQL | Invalid quantity produces `400`; no invalid row is persisted |
| Item comment and request note are normalized | Domain/API | Whitespace-only values become null; length limits are enforced |
| Total is recalculated after add, update and remove | Domain/handler/API | Server total equals snapshot price multiplied by quantity under the agreed rounding rule |
| Product changes after add do not rewrite history | PostgreSQL/API | Details continue to show the item snapshot |
| Update requires current concurrency stamp | API/handler | Missing or stale stamp is rejected; no silent overwrite |
| Two writes use the same stamp | PostgreSQL integration | Exactly one succeeds and the other maps to `409 Conflict` |
| Remove last item from a draft | Domain/API | Draft remains valid and empty; total becomes zero |
| Submitted state cannot be edited | Domain/handler | Covered as a future-state guard without implementing submission in this branch |
| List is scoped and stable | PostgreSQL/API | Only own requests appear, sorted with a deterministic tie-breaker |
| Empty list is explicit | API/frontend | `200` with an empty collection, not an error |
| Frontend handles loading/empty/error | Frontend test | UI remains usable without assuming data exists |
| Frontend handles stale version | Frontend/API test | `409` triggers refresh guidance and does not overwrite local state silently |
| Module registration resolves handlers | Architecture/integration | All draft ports resolve from `AddPurchaseRequestsModule` |

## Domain tests

- `PurchaseRequest.Create` requires valid organization, branch and author ids.
- A new aggregate starts as `Draft` with an initialized non-empty stamp.
- `AddItem` rejects an empty product id, invalid snapshots and duplicate product
  identity.
- `AddItem` accepts valid quantity and recalculates total.
- `UpdateItemQuantity` rejects zero, negative and over-limit quantities.
- `RemoveItem` recalculates total and permits an empty draft.
- Every successful mutation changes the aggregate stamp.
- A non-draft aggregate rejects item mutations. The submission branch will add
  the transition tests that produce such a state.

## PostgreSQL tests

The Testcontainers suite must prove properties that unit tests cannot prove:

- required foreign keys and restrictive relationships to organization/branch/
  author remain valid;
- `(PurchaseRequestId, ProductId)` is unique;
- numeric precision and scale match the agreed quantity and money rules;
- EF `ConcurrencyStamp` is a concurrency token on the request table;
- two independently loaded contexts produce one successful mutation and one
  `DbUpdateConcurrencyException` mapped to application `Conflict`;
- a stale writer cannot change the persisted item or total;
- list projection applies scope, ordering and pagination in PostgreSQL.

## Validation order

1. Domain unit tests.
2. Handler tests with mock/fake focused stores.
3. In-memory API authorization and response tests.
4. PostgreSQL/Testcontainers tests.
5. Frontend tests, typecheck and production build.
6. Release backend build, frontend build and existing regression suites.

Do not mark the database concurrency requirement complete from a mock test
alone. Docker availability is required for the PostgreSQL proof.

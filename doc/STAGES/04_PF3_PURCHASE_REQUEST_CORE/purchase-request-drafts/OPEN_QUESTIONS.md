# Decision gates: purchase-request drafts

These questions are intentionally recorded before implementation. The branch
can use the recommended defaults for an MVP, but the values must be frozen
before creating the EF migration or public request contracts.

| ID | Decision gate | Recommended default | Why it matters | Owner |
| --- | --- | --- | --- | --- |
| PRD-001 | Quantity type and limit | `decimal`, PostgreSQL `numeric(12,3)`, `0 < quantity <= 1,000,000` | Supports kilogram/liter-style units without allowing unbounded values | Product/domain owner |
| PRD-002 | Money calculation | Unit price `numeric(12,2)`; total rounded and persisted to two decimals using one documented rule | Prevents different totals in Domain, SQL and frontend | Product/domain owner |
| PRD-003 | Text limits | Request note `2,000` chars; item comment `1,000` chars; trim and turn blank into null | Protects persistence and keeps API/UI contracts predictable | Product/domain owner |
| PRD-004 | Route scope | `/api/organizations/{organizationId}/purchase-requests`; branch is always derived from current membership | Keeps organization scope explicit without trusting a client BranchId | API/application owner |
| PRD-005 | Duplicate product line | Reject a second line with `409`; user changes quantity through the quantity slice | Gives one clear line per product and a simple database unique index | Product/domain owner |
| PRD-006 | Membership changes after creation | Require the author's active Employee membership in the stored organization and branch for draft mutations | Prevents a moved user from editing a different branch's draft without an explicit transfer rule | Organization/product owner |
| PRD-007 | Catalog selection contract | PF2 exposes a focused organization-scoped read returning ProductId, name, optional code, unit and non-negative price plus active/available state | PF3 must not query or serialize an unfinished catalog model directly | Catalog/PurchaseRequests owners |
| PRD-008 | Draft list paging | `page` defaults to `1`; `pageSize` defaults to `20` and is capped at `100`; order by `UpdatedAt DESC, Id DESC` | Keeps list behavior deterministic and database-friendly | Application owner |
| PRD-009 | Historical product identity | Keep `ProductId` and all agreed snapshots; do not rewrite snapshots when PF2 changes a product | Supports audit/history while retaining a catalog link | Product owner |
| PRD-010 | Empty draft lifecycle | Empty drafts are valid and editable in this branch; submission rejects them in the next branch | Prevents creation and item editing from depending on future workflow rules | Product owner |

## Decisions already made for this branch

- `PurchaseRequest` is a new aggregate, not a rename of `ProjectTask`.
- `PurchaseRequestItem` belongs to the aggregate and changes only through
  aggregate methods.
- Client input never controls status, price or total.
- Every mutation after creation requires the expected `ConcurrencyStamp`.
- HTTP concerns remain in API; Application results remain HTTP-independent.
- Submission, cancellation and status history are outside this branch.

## Blocking checks before coding

1. Confirm PF2's product read contract and exact product fields.
2. Confirm the final quantity and money rules with the product owner.
3. Confirm the route and current-membership convention against the nearest
   organization controller.
4. Confirm whether the existing migration policy accepts request/item check
   constraints in PostgreSQL.

# Slices: PF2 Catalog

Implement one business result at a time. Each slice must have one clear
invariant, one cheapest proving test and an independently valid checkpoint.

```text
01 CreateUnitOfMeasure [Implemented; PostgreSQL validation pending]
    -> create one active organization-scoped unit with a normalized symbol

02 CreateProductCategory [Planned]
    -> create one active organization-scoped category with a normalized name

03 ArchiveOrRestoreReferenceData [Planned]
    -> change reference lifecycle without deleting values used by products

04 CreateProduct [Planned]
    -> create a product using active category and unit references

05 UpdateProduct [Planned]
    -> change product data while preserving identifier and history boundaries

06 SetProductAvailability [Planned]
    -> make a product selectable or temporarily unavailable

07 ArchiveProduct [Planned]
    -> remove a product from new drafts without deleting its identity

08 ListCatalogProducts [Planned]
    -> return database-filtered, sorted and paginated catalog data

09 GetCatalogProductDetails [Planned]
    -> return one organization-scoped product and its current references
```

## Dependency order

```text
01 + 02
    -> 03
        -> 04
            -> 05 + 06 + 07
                -> 08 + 09
                    -> frontend catalog workflow
```

Do not start product creation until category and unit reference rules are
implemented and protected by the database. Do not add purchase-request item
implementation to PF2; PF3 consumes the catalog through a focused read
contract and stores its own historical snapshot.

`CreateUnitOfMeasure` is implemented across Domain, Application,
Infrastructure and API. Its PostgreSQL/Testcontainers tests are present but
cannot run locally until Docker Engine is available.

## Shared rules for every slice

- Catalog records are scoped to one organization and shared by its branches.
- `BranchId` does not belong on PF2 catalog entities.
- Backend authorization is required even when the frontend hides an action.
- Stores return application data or persistence outcomes, not HTTP results.
- Domain entities own single-record state invariants.
- Handlers own cross-record organization and active-reference checks.
- PostgreSQL owns unique constraints, foreign keys and decimal precision.
- Application duplicate checks improve messages; they do not replace database constraints.
- Existing code must be reconciled before a new entity, handler, store or module registration is created.

## Detailed plans

- [01 CreateUnitOfMeasure](01_catalog-management.md)

The remaining slice documents should be created from the shared slice template
after the preceding checkpoint has been validated.

## Validation order

1. Domain and handler unit tests.
2. In-memory API authorization and contract tests.
3. PostgreSQL tests for indexes, foreign keys, precision and pagination.
4. Frontend tests, typecheck and production build.
5. Full backend and frontend validation.

Docker-dependent PostgreSQL tests are required for the database guarantees. A
local environment failure is an execution blocker, not evidence that the
database constraint works.

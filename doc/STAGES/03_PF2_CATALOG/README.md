# Stage PF2: Catalog

- Status: In progress
- Branch: `feature/catalog-management`
- Roadmap source: [PF2 Catalog](../../ROADMAP/PROCUREFLOW/03_PF2_CATALOG.md)
- Product boundary: organization-wide catalog shared by its branches

## Purpose

PF2 provides the small reference catalog required by future purchase-request
drafts. It stores product identity, category, unit of measure and an
indicative unit price. It is not an inventory, warehouse, marketplace or
pricing engine.

The catalog is shared by branches inside one organization. PF2 does not add a
`BranchId` to catalog records and does not calculate branch-specific prices.

The `CreateUnitOfMeasure` slice is implemented. Domain, handler and in-memory
API tests pass, and PostgreSQL integration validation passes with Docker
Testcontainers.

## Stage-level decisions

### 1. Reference data is explicit

`ProductCategory` and `UnitOfMeasure` are persisted reference entities. They
are not represented only by enums because an organization can add, rename or
archive values without a code deployment.

`UnitOfMeasure` is a label used by a product, for example `piece`, `kilogram`
or `hour`. PF2 does not implement unit conversion, conversion factors or a
complete measurement-science model.

### 2. Product lifecycle has two independent dimensions

```text
Catalog state: Active | Archived
Availability:  Available | Unavailable
Selectable for a new request: Active AND Available
```

An archived product remains a valid historical reference, but cannot be added
to a new purchase-request draft. An unavailable product remains in the catalog
for authorized read views, but cannot be selected for a new draft.

The same lifecycle rule applies to categories and units of measure where the
reference is used to create or update a product:

- an archived reference cannot be assigned to a new or updated product;
- existing products keep their reference for history and read models;
- the initial product-selection query must exclude products whose category or
  unit of measure is archived;
- archiving a reference does not silently rewrite or delete existing products.

### 3. History is owned by the purchase-request module

PF2 owns the current product record. PF3 owns the purchase-request item and
must copy the product data used at selection time into a snapshot:

```text
ProductId
ProductNameSnapshot
ProductCodeSnapshot
UnitOfMeasureSnapshot
UnitPriceSnapshot
```

The purchase-request history must not depend on the current product name, code,
unit or price. PF2 does not implement `PurchaseRequestItem`; this is a cross-
stage contract that PF3 must honor.

### 4. Normalization and uniqueness

All user-entered reference and product text is trimmed before validation. The
following comparisons are case-insensitive:

- category name within one organization and active scope;
- unit-of-measure symbol within one organization and active scope;
- product code within one organization when a code is provided.

An empty optional product code is stored as `NULL`. A product code is unique
across both active and archived products so that historical identifiers are not
reused ambiguously. An archived category or unit can be replaced by a new
active value with the same normalized name or symbol; reactivating the old
record then returns a conflict if the active value already exists.

The application performs a friendly duplicate check, but PostgreSQL remains
the final owner of uniqueness through unique indexes and the handler maps a
concurrent violation to `409 Conflict`.

### 5. Money and time

The product unit price is a non-negative `decimal` with an explicit PostgreSQL
`numeric(12,2)` mapping unless a later product decision changes the required
precision. The mapping must be covered by a PostgreSQL integration test.

All timestamps are stored as UTC `timestamp with time zone` values. Catalog
records carry the identifier of the user who created and last changed them,
using the repository's existing audit-field convention after verification.

### 6. Authorization

| Operation | Employee | Manager | Procurement | Global Admin |
| --- | --- | --- | --- | --- |
| Read active catalog | Yes | Yes | Yes | Yes |
| Read archived catalog data for management | No | No | Yes | Yes |
| Create or update reference data | No | No | Yes | Yes |
| Archive or restore reference data | No | No | Yes | Yes |
| Create or update products | No | No | Yes | Yes |
| Archive or change product availability | No | No | Yes | Yes |

The catalog is organization-scoped. `Employee`, `Manager` and `Procurement`
permissions come from the current organization membership; `Admin` follows the
existing platform administration convention. Do not move business roles into
the global `UserRole` enum and do not make the frontend the security boundary.

## Data model boundary

| Record | Required data | Lifecycle | Scope | Notes |
| --- | --- | --- | --- | --- |
| `ProductCategory` | `Name` | Active/Archived | Organization | Active names are unique after normalization |
| `UnitOfMeasure` | `Name`, `Symbol` | Active/Archived | Organization | Active symbols are unique after normalization |
| `Product` | `Name`, `CategoryId`, `UnitOfMeasureId`, `UnitPrice` | Active/Archived + Available/Unavailable | Organization | `CatalogNumber` is optional; no stock quantity |

All foreign keys to category and unit of measure use restrictive delete
behavior. The PF2 MVP exposes archive operations rather than hard-delete
operations. Database foreign keys remain a defense-in-depth protection for
future maintenance code.

## Planned API surface

The route shape follows the existing organization-scoped API convention. The
exact authorization policy and response wrapper must be reconciled with the
nearest controller before implementation.

| Use case | Method and route | Allowed callers |
| --- | --- | --- |
| Create category | `POST /api/organizations/{organizationId}/catalog/categories` | Procurement, Admin |
| Create unit | `POST /api/organizations/{organizationId}/catalog/units-of-measure` | Procurement, Admin |
| Create product | `POST /api/organizations/{organizationId}/catalog/products` | Procurement, Admin |
| Update product | `PUT /api/organizations/{organizationId}/catalog/products/{productId}` | Procurement, Admin |
| Archive product | `POST /api/organizations/{organizationId}/catalog/products/{productId}/archive` | Procurement, Admin |
| Change availability | `POST /api/organizations/{organizationId}/catalog/products/{productId}/availability` | Procurement, Admin |
| List products | `GET /api/organizations/{organizationId}/catalog/products` | Organization members |
| Get product details | `GET /api/organizations/{organizationId}/catalog/products/{productId}` | Organization members |

List endpoints must filter, sort and paginate in PostgreSQL. The API must use
an allow-list for sort fields and must define a stable tie-breaker, normally
`Id`, after the requested sort field.

## Slice order

The implementation order keeps reference data valid before products can point
to it:

```text
Reference data
    -> products
        -> product read models
            -> frontend catalog workflow
                -> PF3 purchase-request integration
```

See [PF2 slices](catalog-management/SLICES.md) for the independently
validatable operations.

## Out of scope

- inventory quantities, reservations or stock movements;
- suppliers, marketplace data or ERP import;
- product variants and multiple product images;
- discounts, tax calculation or branch-specific price lists;
- unit conversion;
- advanced full-text search;
- implementation of `PurchaseRequest` or `PurchaseRequestItem`.

## Stage definition of done

- [ ] Categories, units and products have separate persisted models.
- [ ] Organization scope and role authorization are enforced by the backend.
- [ ] Archive and availability semantics are explicit and tested.
- [ ] Uniqueness and foreign-key rules are protected in PostgreSQL.
- [ ] Product price precision is explicit and tested against PostgreSQL.
- [ ] Product lists paginate and filter in the database.
- [ ] The frontend covers loading, empty, error and archive-confirmation states.
- [ ] PF3 has a documented snapshot contract before products can enter request drafts.
- [ ] Backend and frontend builds and focused tests pass.

## Teach-back questions

1. Why is the catalog organization-scoped instead of branch-scoped?
2. What is the difference between `Archived` and `Unavailable`?
3. Why can an archived product remain in history without being selectable?
4. Which data must PF3 snapshot when a product enters a request draft?
5. Which uniqueness rules require PostgreSQL protection in addition to validation?

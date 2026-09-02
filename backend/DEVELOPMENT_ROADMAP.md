# ProcureFlow Development Roadmap

## Purpose

This file is the backend-visible index for the ProcureFlow product roadmap. The canonical,
detailed product documents live under
[`../doc/ROADMAP/PROCUREFLOW/`](../doc/ROADMAP/PROCUREFLOW/00_PRODUCT_ROADMAP_OVERVIEW.md).

The existing V1-V8 roadmap remains a record of the starter's technical maturity. ProcureFlow
now uses that foundation to build a focused multi-branch purchase-request product at junior+
level. Backend correctness, domain rules, authorization and PostgreSQL integration tests remain
the primary learning goals.

## Current priority

**PF1: Organization, branches and resource access** is the next implementation stage.

PF0 defines the MVP, workflow and implementation order. The application already contains the
technical foundation: authentication, notifications, file storage, observability, Docker, CI/CD
and automated tests. The existing Projects and ProjectTasks domain remains available only until
the ProcureFlow workflow replaces it and passes the PF5 release-level tests.

## Current progress

As of **2026-09-02**. Product progress is tracked separately from the completed technical
foundation. Status is based on a stage Definition of Done, not the number of files or endpoints.

| Stage | Status | Next result |
|---|---|---|
| PF0 | Complete | Product scope, workflow and branch order are defined. |
| PF1 | Next | Organization, branches and business memberships. |
| PF2 | Planned | Product catalog with reference tables. |
| PF3 | Planned | Purchase-request drafts, items and submission. |
| PF4 | Planned | Monthly budgets, approvals and race-condition handling. |
| PF5 | Planned | Fulfillment, attachments, notifications, dashboard and E2E. |
| PF6 | Planned | Remove the demo domain and pass the `v1.0.0` release gate. |
| PF7 | Optional | At most one or two post-MVP extensions. |

## Stage index

| Stage | Focus | Detailed document |
|---|---|---|
| PF0 | Product scope and domain decisions | [01_PF0_SCOPE_AND_DECISIONS.md](../doc/ROADMAP/PROCUREFLOW/01_PF0_SCOPE_AND_DECISIONS.md) |
| PF1 | Organization, branches and access | [02_PF1_ORGANIZATION_AND_ACCESS.md](../doc/ROADMAP/PROCUREFLOW/02_PF1_ORGANIZATION_AND_ACCESS.md) |
| PF2 | Catalog reference data and products | [03_PF2_CATALOG.md](../doc/ROADMAP/PROCUREFLOW/03_PF2_CATALOG.md) |
| PF3 | Purchase-request aggregate and submission | [04_PF3_PURCHASE_REQUEST_CORE.md](../doc/ROADMAP/PROCUREFLOW/04_PF3_PURCHASE_REQUEST_CORE.md) |
| PF4 | Monthly budgets and approvals | [05_PF4_APPROVALS_AND_BUDGETS.md](../doc/ROADMAP/PROCUREFLOW/05_PF4_APPROVALS_AND_BUDGETS.md) |
| PF5 | Fulfillment and product completeness | [06_PF5_PRODUCT_COMPLETENESS.md](../doc/ROADMAP/PROCUREFLOW/06_PF5_PRODUCT_COMPLETENESS.md) |
| PF6 | Demo cleanup and `v1.0.0` release | [07_PF6_CLEANUP_AND_RELEASE.md](../doc/ROADMAP/PROCUREFLOW/07_PF6_CLEANUP_AND_RELEASE.md) |
| PF7 | Optional post-MVP extensions | [08_PF7_POST_MVP_OPTIONS.md](../doc/ROADMAP/PROCUREFLOW/08_PF7_POST_MVP_OPTIONS.md) |
| Learning workflow | How to work through each stage | [08_LEARNING_WORKFLOW.md](../doc/ROADMAP/08_LEARNING_WORKFLOW.md) |
| Modular VSA checklist | Definition of Done for modules and slices | [MODULAR_VSA_MODULE_CHECKLIST.md](../doc/MODULAR_VSA_MODULE_CHECKLIST.md) |

The complete branch order is in the
[product roadmap overview](../doc/ROADMAP/PROCUREFLOW/00_PRODUCT_ROADMAP_OVERVIEW.md).
The previous [technical foundation roadmap](../doc/ROADMAP/00_ROADMAP_OVERVIEW.md)
remains available as reference.

## Execution rules

- Implement one coherent feature or hardening topic per branch.
- Follow the canonical PF branch order unless a documented dependency changes.
- Keep the modular monolith unless measurements or operational constraints justify a different boundary.
- Add tests and documentation with each backend change.
- Prefer the smallest change that protects a real invariant or solves a measured problem.
- Do not add technologies only for a CV checklist.
- Treat frontend changes as support for backend workflows unless the task explicitly targets frontend learning.
- Validate the relevant build and tests before considering a stage item complete.
- Keep Projects and ProjectTasks until the ProcureFlow critical flow passes PF5 validation.

## Recommended branch names

For the documentation work:

```text
docs/procureflow-product-roadmap
```

For the immediate implementation work:

```text
feature/organization-branches
```

Then continue with `feature/branch-access-control`, `feature/catalog-management`,
`feature/purchase-request-drafts` and the remaining branches listed in the canonical overview.

## Learning model

The project owner implements the roadmap one branch at a time. The assistant supports planning,
targeted explanations, code review, debugging and verification without replacing the owner's
understanding of the changed code.

Junior+ value comes from correct domain rules, resource authorization, transactions, concurrency
and tests. It is not a checklist of additional technologies.

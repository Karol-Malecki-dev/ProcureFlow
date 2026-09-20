# Branch 01: Organization and branches

- Status: Completed
- Branch: `feature/organization-branches`
- Stage: `PF1`
- Source of truth: [`doc/ROADMAP/PROCUREFLOW/02_PF1_ORGANIZATION_AND_ACCESS.md`](../../../ROADMAP/PROCUREFLOW/02_PF1_ORGANIZATION_AND_ACCESS.md)

## Purpose

This branch creates the organization and branch context used by the next
business features. It does not implement memberships or business roles.

## Scope

```text
Organization
`-> Branches
    +-> Create
    +-> List
    +-> Details
    +-> Update
    `-> Archive
```

## Main rules

- A branch belongs to exactly one organization.
- Branch name and code are normalized before comparison.
- Branch name and code are unique inside one organization.
- An archived branch is not deleted when historical data refers to it.
- An archived branch cannot receive new business data.
- An identifier in the URL is not proof of authorization.

## How to use this folder

This folder is a reference for the completed branch. Use it to understand the
shape of a finished feature before starting Branch 02:

1. Read `FEATURE_BRIEF.md` to understand the business problem.
2. Read `FILE_PLAN.md` to see the layer-by-layer implementation order.
3. Read `TEST_PLAN.md` to see how each important rule is proved.
4. Read `SLICES.md` to see how a large branch is split into smaller slices.
5. Use `OPEN_QUESTIONS.md` to record any missing decision found during review.

Do not copy the code mechanically. First compare the nearest existing
implementation and keep only the parts that fit the new use case.

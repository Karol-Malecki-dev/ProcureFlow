# Slices: organization and branch management

A slice is one small behavior that can be implemented, tested and explained.
The branch was split into these steps:

```text
01 CreateBranch
    -> create valid state
    -> persist it
    -> return the created branch

02 ListBranches + GetBranchDetails
    -> read only inside organization scope
    -> define stable ordering

03 UpdateBranch
    -> validate new values
    -> preserve organization ownership
    -> protect uniqueness

04 ArchiveBranch
    -> change active state
    -> keep historical references
    -> reject future business use
```

## Slice template

For a new slice, write only these sections first:

```text
Goal:
Actor:
Resource scope:
Success:
Invariant:
Expected failures:
Cheapest proving test:
Out of scope:
```

Then ask AI for `PLAN ONLY`, approve the boundaries and add the exact files to
`FILE_PLAN.md`.

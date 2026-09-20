# PF1: working area

- Status: In progress
- Stage: `PF1 - organization, branches and access`
- Canonical scope: [`doc/ROADMAP/PROCUREFLOW/02_PF1_ORGANIZATION_AND_ACCESS.md`](../../ROADMAP/PROCUREFLOW/02_PF1_ORGANIZATION_AND_ACCESS.md)
- Current next branch: `feature/branch-access-control`

## Purpose

This folder is the working area for PF1. The canonical roadmap describes the
product scope. The branch folders below describe how one branch is planned,
implemented and validated.

## Structure

```text
02_PF1_ORGANIZATION_AND_ACCESS/
|
+-- README.MD              <- stage index and status
+-- FEATURE_BRIEF.MD       <- boundary between Branch 01 and Branch 02
+-- FILE_PLAN.MD           <- documentation and code planning map
+-- TEST_PLAN.MD           <- behavior-to-test map
+-- OPEN_QUESTIONS.MD      <- shared unresolved questions
|
+-- 01_organization-branches/
|   +-- README.md
|   +-- FEATURE_BRIEF.md
|   +-- FILE_PLAN.md
|   +-- TEST_PLAN.md
|   +-- OPEN_QUESTIONS.md
|   `-- SLICES.md
|
+-- 02_branch-access-control/
|   +-- README.md
|   +-- FEATURE_BRIEF.md
|   +-- FILE_PLAN.md
|   +-- TEST_PLAN.md
|   +-- OPEN_QUESTIONS.md
|   `-- SLICES.md
|
`-- slices/                  <- short navigation maps
```

## Status

```text
PF1
|
+-- Branch 01: organization and branches  -> Completed
|
`-- Branch 02: branch access control      -> Next
```

## How to work

1. Read the canonical PF1 document.
2. Open the folder of the active branch.
3. Read `FEATURE_BRIEF.md` and resolve `OPEN_QUESTIONS.md`.
4. Ask AI for `PLAN ONLY` using the brief, file plan and test plan.
5. Implement one slice from `SLICES.md`.
6. Run the cheapest proving test before moving to the next slice.
7. Update the branch status only after validation.

Do not duplicate the full product roadmap here. Link to it and keep this folder
focused on execution, learning and decisions for PF1.

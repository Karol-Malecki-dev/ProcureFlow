# PF1 file plan

- Status: Working index
- Rule: confirm every path against the current repository before creating a file

## Documentation structure

```text
PF1
|
+-- stage overview
|   +-- README.MD
|   +-- FEATURE_BRIEF.MD
|   +-- FILE_PLAN.MD
|   +-- TEST_PLAN.MD
|   `-- OPEN_QUESTIONS.MD
|
+-- Branch 01
|   `-- 01_organization-branches/
|       +-- brief
|       +-- file plan
|       +-- test plan
|       +-- questions
|       `-- slices
|
`-- Branch 02
	`-- 02_branch-access-control/
		+-- brief
		+-- file plan
		+-- test plan
		+-- questions
		`-- slices
```

## Code structure used by a vertical slice

```text
Domain
	-> entity/value object owns valid state
		-> Application handler coordinates the use case
			-> Infrastructure store persists exact data
				-> API maps HTTP input and result
					-> Tests prove the behavior
```

## PF1 code areas

```text
backend/
+-- Domain/Entities/
|   +-- Organization.cs
|   +-- Branch.cs
|   `-- OrganizationMembership.cs
|
+-- Domain/Enums/
|   `-- BusinessRole.cs
|
+-- Application/Modules/Organizations/
|   +-- branch use cases
|   `-- membership/access use cases
|
+-- Infrastructure/Modules/Organizations/
|   `-- EF stores and persistence handlers
|
+-- API/Modules/Organizations/
|   `-- requests, responses, validators and controllers
|
+-- IntegrationTests/
|   `-- organization and access scenarios
|
`-- UnitTests/
	`-- domain and application behavior
```

The branch-specific file plans contain the detailed order. This stage file is
only the map; it should not become a second copy of every implementation plan.

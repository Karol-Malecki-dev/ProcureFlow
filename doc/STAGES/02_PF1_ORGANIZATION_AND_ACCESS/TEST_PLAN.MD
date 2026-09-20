# PF1 test plan

- Status: Working index
- Main risk: authenticated users can reach data outside their organization or branch

## Behavior map

| Area | Main proof | Detailed plan |
|---|---|---|
| Organization and branches | ownership, uniqueness and archive behavior | [`01_organization-branches/TEST_PLAN.md`](01_organization-branches/TEST_PLAN.md) |
| Membership state | valid role and branch combinations | [`02_branch-access-control/TEST_PLAN.md`](02_branch-access-control/TEST_PLAN.md) |
| Branch isolation | manager assigned to A cannot access B | Branch 02 PostgreSQL/API tests |
| Database rules | foreign keys, unique active membership and indexes | PostgreSQL integration tests |
| Public API | `401`, `403`, `404`, validation and conflict behavior | API integration tests |
| Frontend | loading, empty, error and forbidden action states | Focused frontend tests |

## Validation order

```text
Domain unit tests
	-> Application/handler tests
		-> API integration tests
			-> PostgreSQL constraint tests
				-> Frontend tests
					-> Release build and broader tests
```

## Completion rule

Do not mark PF1 complete because every endpoint exists. The important business
rules must be demonstrated by tests at the correct boundary, especially branch
isolation, current membership state and database constraints.

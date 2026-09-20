# Slice map: organization context

This is a short navigation note, not a second implementation plan.

```text
Organization
	`-> owns Branches
		`-> provides the scope for later memberships and business data
```

Detailed branch documentation: [`../01_organization-branches/`](../01_organization-branches/README.md).

The key learning point is ownership: a branch belongs to an organization, so
queries and constraints must preserve that relationship.

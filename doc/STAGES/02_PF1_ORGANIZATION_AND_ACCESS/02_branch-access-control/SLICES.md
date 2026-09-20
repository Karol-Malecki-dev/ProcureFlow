# Slices: branch access control

Implement one slice at a time. Each slice must have one clear result and one
cheapest proving test.

```text
01 AssignMembership [Implemented]
    -> load active user, organization and branch
    -> validate role + branch combination
    -> persist one active membership

02 ListMemberships [Implemented]
    -> list memberships in organization scope
    -> expose role, user and branch information

03 ChangeMembershipRoleOrBranch [Implemented]
    -> validate the new combination
    -> preserve membership history
    -> keep organization ownership unchanged

04 RemoveOrDeactivateMembership [Implemented]
    -> make current access stop working
    -> decide whether history remains queryable

05 GetCurrentMembership [Implemented]
    -> resolve the authenticated user's active context
    -> provide one port for later catalog and request handlers
```

PostgreSQL execution of the constraint and concurrency checks is blocked in the
current environment because Docker Engine is unavailable. The tests remain in
the integration project and the project compiles.

Detailed first-slice plan: [01_assign-membership.md](01_assign-membership.md)

The slice plan uses `AssignMembership` as the business name and preserves the
existing `CreateMembership` technical operation until a coordinated rename is
actually justified.

## Slice writing template

Copy this small shape for a new slice:

```text
# Slice: <name>

Status: Draft | In progress | Validated | Blocked
Actor:
Resource:
Goal:
Success:
Invariant:
Failure paths:
Cheapest proving test:
Files to confirm:
Out of scope:

Flow:
Request -> API -> Handler -> Domain/Store -> PostgreSQL -> Result
```

## Recommended first slice

The implemented vertical slice covers the full MVP membership workflow. The
first operation remains `AssignMembership`, and it forced the most important
decisions early:

- who may assign a membership;
- whether the user is active;
- whether the organization and branch are active;
- which role/branch combinations are valid;
- how duplicate membership is protected;
- which test proves branch isolation.

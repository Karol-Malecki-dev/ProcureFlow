# Slice map: memberships and access

```mermaid
flowchart LR
    Assign["01 Assign membership"] --> Change["02 Change role or branch"]
    Change --> Resolve["03 Resolve current membership"]
    Resolve --> Deactivate["04 Deactivate/remove access"]
```

The most important proof is branch isolation:

```mermaid
flowchart LR
    Manager["Manager membership"] --> BranchA["Branch A"]
    BranchA --> RequestB["Request for Branch B"]
    RequestB --> Rejected["Rejected"]
```

This map uses Mermaid because it shows slice order and the security proof
quickly. The detailed slice document must still define the exact API contract,
file paths, invariants and tests in text and tables.

Detailed plan: [`../02_branch-access-control/SLICES.md`](../02_branch-access-control/SLICES.md).

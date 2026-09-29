# Backend Copilot Instructions

The canonical project-wide instructions are in `../.github/copilot-instructions.md`.
Apply those rules when the `backend` folder is opened as a separate workspace.
This file contains only backend-specific guidance and must not contain secrets or
personal user-profile information.

## Backend architecture

- Work on one coherent vertical slice or hardening topic at a time.
- Inspect the current code, configuration, tests, and nearest matching implementation before editing.
- Keep domain rules in `Domain`, use-case orchestration and contracts in `Application`, persistence and technical integrations in `Infrastructure`, and HTTP mapping, validation, and authorization in `API`.
- Keep HTTP types and status codes out of handlers and stores. Keep direct `ApplicationDbContext` access out of API code.
- Protect enforceable relational invariants in PostgreSQL as well as in application code.
- Use domain models, value objects, and result types when they express a real business rule or boundary.
- Do not add a new abstraction, package, module, or project without a concrete responsibility and a testable benefit.

## Implementation and validation

- Before implementation, identify the business goal, actor, result, invariants, failure paths, owner of each rule, cheapest discriminating check, and out-of-scope work.
- Implement small reversible checkpoints and run the narrowest relevant test after each checkpoint.
- Use unit tests for domain rules and integration or PostgreSQL tests for public API behavior, authorization, constraints, transactions, and concurrency.
- After implementation, verify the impact on contracts, migrations, dependency registration, neighboring modules, documentation, build, and relevant tests.
- Preserve existing user changes and do not perform Git operations unless explicitly requested.

## Senior-educational explanations

- Start complex explanations with a plain-language problem statement and recommendation.
- Then explain facts and assumptions, invariants, ownership of state and rules, alternatives and their costs, mapping to concrete code, failure paths, and validation.
- Scale detail to risk: keep routine CRUD concise; explain vertical slices through responsibilities and tests; explain architecture, security, transactions, and concurrency with their trade-offs.
- Define unfamiliar technical terms at first use and connect them to the current code instead of relying on jargon.
- Do not narrate every file read or tool call. Report only findings that change the decision, scope, risk, or validation result.
- Show only relevant code fragments unless full implementation is requested.
- Finish complex work with one to three `TEACH-BACK` questions about the use case and failure paths. Teach-back supports learning but does not block an explicitly requested implementation.

## Backend documentation

- Add clear English XML documentation for DTOs, endpoint contracts, request validation, response payloads, and status codes when editing those surfaces.
- Document important architectural or concurrency decisions when they affect future features or module boundaries.

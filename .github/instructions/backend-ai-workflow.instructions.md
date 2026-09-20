---
description: "Use when working on the ASP.NET, C#, EF Core, PostgreSQL, API, Clean Architecture, Vertical Slice Architecture, testing, migration, authorization, or concurrency code under backend."
name: "Backend AI Workflow"
applyTo: "backend/**/*"
---

# Backend AI Workflow

This instruction complements `.github/copilot-instructions.md` and the backend
workspace instructions in `backend/.github/copilot-instructions.md`. The full
workflow is documented in `doc/AI_ASSISTED_DEVELOPMENT_WORKFLOW.md`.

## Working method

- Work on one coherent vertical slice or one hardening topic at a time.
- Inspect the current implementation, configuration, tests, and nearest matching pattern first.
- Begin in `PLAN ONLY` mode unless the user explicitly requests implementation.
- Separate facts, assumptions, and unknowns.
- Before editing, state the business goal, actor, result, invariants, rule owner,
  error statuses, atomic write scope, and cheapest test that could disprove the hypothesis.
- Show at most a few realistic options, their costs, and the smallest recommended option.
- After approval, implement small checkpoints and run the narrowest relevant validation after each one.
- Preserve user changes and do not perform Git operations without an explicit request.

## Layer responsibilities

- `Domain` owns rules of an entity or aggregate and does not depend on HTTP, EF Core, or PostgreSQL.
- `Application` owns commands/queries, results, ports, handler contracts, and use-case orchestration.
- `Infrastructure` owns EF Core, persistence configuration, migrations, stores, and port implementations.
- `API` owns binding, HTTP request/response models, input validation, authorization, and result-to-status mapping.
- Handlers do not return HTTP types, stores do not return HTTP statuses, and controllers do not own domain rules.
- API code should not access `ApplicationDbContext` directly when a focused port is sufficient.
- Cross-module access goes through an explicit port, identifier, or application workflow.
- Rules enforceable by the database should also be protected with a PostgreSQL constraint or index.

## Vertical-slice order

1. Domain rule and unit test.
2. EF mapping, constraints, and migration when persistence changes.
3. Application command/query, result, port, and handler contract.
4. Infrastructure store and handler implementation.
5. API request, response, validation, authorization, and controller.
6. API or PostgreSQL tests, documentation, and broader validation.
7. Frontend only after the API contract is stable.

## Validation and learning

- Domain or handler changes require focused unit tests and the appropriate build.
- Public API or authorization changes require an integration test.
- Constraints, transactions, or concurrency require a PostgreSQL test.
- Do not claim PostgreSQL validation passed based only on unit tests.
- After implementation, explain data flow, failure paths, risks, validation results, and gaps.
- Finish with a short `TEACH-BACK`: ask the user to explain the use case without looking at generated code.
- Balance `Delivery Mode` and `Training Mode`; AI may write boilerplate, but the user should reproduce the core rules, tests, and flow independently.

## Conversation modes

- `PLAN ONLY`: analysis and plan, no edits.
- `IMPLEMENT`: small implementation after boundaries are approved.
- `REVIEW`: risks, defects, and missing tests only; no automatic edits.
- `DEBUG`: hypothesis, cheapest check, and local repair.
- `TEACH-BACK`: questions that test understanding without giving the answer first.

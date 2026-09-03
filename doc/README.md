# Documentation Map

This directory documents both the active ProcureFlow product work and the inherited
technical foundation. These are related, but they have different sources of truth.

## Sources of Truth

| Documentation area | Purpose | Authority |
|---|---|---|
| [ProcureFlow product roadmap](ROADMAP/PROCUREFLOW/00_PRODUCT_ROADMAP_OVERVIEW.md) | Product scope, stage order and current implementation priority | Canonical for product work |
| [Backend roadmap index](../backend/DEVELOPMENT_ROADMAP.md) | Backend-facing summary of the product roadmap | Derived product index |
| [Architecture](ARCHITECTURE.md) and setup guides | Current implementation and development conventions | Current-code reference |
| [Technical foundation roadmap](ROADMAP/00_ROADMAP_OVERVIEW.md) | Maturity history of the starter foundation | Historical and reference material |
| [Audits](Audits/) | Evidence captured at a stated date and scope | Point-in-time evidence |

When two documents appear to prescribe different next steps, the ProcureFlow product
roadmap controls the implementation order. Current code and tests control claims about
what is already implemented.

## Current State

- PF0 is complete.
- PF1 organization, branches and resource access is the next product stage.
- Authentication, PostgreSQL, notifications, file storage, observability, Docker and
  CI/CD form the existing technical foundation.
- `Projects` and `ProjectTasks` are a temporary demonstration domain. They remain
  operational until ProcureFlow passes PF5 validation and are removed in PF6.
- The `V1-V8` documents describe how the technical foundation matured. They are not
  the active product backlog and their version labels are not semantic release tags.

## Naming Policy During Migration

`ProcureFlow` is the product name used in user-facing and product documentation.
Inherited runtime identifiers such as JWT issuer/audience values, Data Protection
application name, container/volume names, image names and deployment paths remain
unchanged until a separately validated PF6 migration.

Changing those identifiers early can invalidate sessions or protected data, create new
Docker volumes instead of reusing existing data, and break deployment or backup scripts.
Documentation must therefore explain the distinction instead of implying that a broad
rename is harmless.

## Reading Paths

### Implement the next product stage

1. [ProcureFlow product roadmap](ROADMAP/PROCUREFLOW/00_PRODUCT_ROADMAP_OVERVIEW.md)
2. [PF1: organization and access](ROADMAP/PROCUREFLOW/02_PF1_ORGANIZATION_AND_ACCESS.md)
3. [Adding features](ADDING_FEATURES.md)
4. [Modular VSA checklist](MODULAR_VSA_MODULE_CHECKLIST.md)
5. [Learning workflow](ROADMAP/08_LEARNING_WORKFLOW.md)

### Understand or run the current application

1. [Getting started](GETTING_STARTED.md)
2. [Architecture](ARCHITECTURE.md)
3. [Backend setup](BACKEND_SETUP.md)
4. [Frontend setup](FRONTEND_SETUP.md)
5. [Docker Compose](../docker/DOCKER_COMPOSE.md)
6. [CI/CD](CI_CD.md)

### Review security and operations

- [JWT architecture](JWT_ARCHITECTURE.md)
- [Email and 2FA flows](EMAIL_2FA_FLOWS.md)
- [Attachment operations](ATTACHMENT_OPERATIONS.md)
- [V5 deployment runbook](V5_DEPLOYMENT_RUNBOOK.md)
- [V5 release gate](V5_RELEASE_GATE.md)

## Maintenance Rule

Product roadmap documents describe planned behavior. Architecture and setup documents
must describe the behavior that is actually present in the worktree. Update both only
after implementation and relevant validation prove a planned capability. Historical
ADRs and audits should be retained with their original scope rather than rewritten to
look as though they decided a later ProcureFlow concern.
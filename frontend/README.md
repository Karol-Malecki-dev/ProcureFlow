# ProcureFlow Frontend

This React 19 and TypeScript application is the UI for ProcureFlow. It reuses the
existing authenticated shell and backend-driven runtime configuration while the
temporary `Projects` and `ProjectTasks` screens are replaced incrementally by the
PF1-PF5 product workflows.

The canonical architecture and development guide is
[Frontend Setup](../doc/FRONTEND_SETUP.md). Product scope and implementation order
are defined in the
[ProcureFlow roadmap](../doc/ROADMAP/PROCUREFLOW/00_PRODUCT_ROADMAP_OVERVIEW.md).
The concrete file-by-file workflow for a new product slice is in the
[ProcureFlow implementation playbook](../doc/ROADMAP/PROCUREFLOW/09_IMPLEMENTATION_PLAYBOOK.md).

## Requirements

- Node.js 22.x
- npm with the repository lockfile

## Available Scripts

```powershell
npm start
npm test
npm run test:once
npm run build
npm run test:e2e
npm run test:e2e:ui
npm run preview
```

## Runtime Config

The app reads `GET /api/runtime-config` during bootstrap.

The central hook is `useFeatureAvailability()`. Product-neutral flags control:

- the navbar quick search bar
- dashboard visibility
- admin navigation
- users navigation
- email-related UI sections

Temporary demo-domain flags control projects, project archiving and task assignment.
They are removed with the legacy screens in PF6 and do not replace server-side
authorization.

The app shell shows a loading gate until both auth and runtime config are ready.

## Local Development

Set the API URL in `frontend/.env.development.local`:

```env
VITE_API_URL=http://localhost:5000
```

## Quick Search

When `GlobalSearchEnabled` is true, the navbar shows the current `Ctrl+K` quick
search. Product data search is added only when a ProcureFlow stage defines its
authorization and result contract.

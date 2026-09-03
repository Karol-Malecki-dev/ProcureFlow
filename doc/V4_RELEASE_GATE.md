# V4 Release Gate

> [!NOTE]
> This is the repository gate for the inherited technical foundation and the
> temporary demo domain. It remains useful as a regression gate, but it is not the
> ProcureFlow `v1.0.0` product gate. PF6 requires this validation plus the PF5
> purchase-request browser workflow and target-environment evidence.

Run from PowerShell with Docker Desktop available:

```powershell
.\scripts\Invoke-E2ETests.ps1
```

The gate starts a clean application stack and verifies:

- backend Release build and all .NET tests;
- frontend unit tests and production build;
- deployed backend and frontend health;
- HTTP smoke tests;
- Playwright browser workflows against Docker Compose and Mailpit.

On failure, preserve test traces and container logs before cleanup. A release is accepted
only when the script exits with code `0`, the worktree contains the expected migrations,
and attachment backup/reconciliation procedures have been exercised in the target
environment.

Production object storage and malware scanning remain deployment decisions and cannot
be certified by the provider-neutral local gate. Their platform-specific restore and
quarantine tests are mandatory before a production readiness claim.
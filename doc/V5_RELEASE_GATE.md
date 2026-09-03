# V5 Release Gate

V5 is accepted only after both repository validation and target-environment validation.

> [!IMPORTANT]
> V5 validates deployment and operational readiness. It does not by itself prove
> that the ProcureFlow business scope is complete. Before `v1.0.0`, the staging gate
> must use the PF5 critical purchase-request workflow; project/task scenarios remain
> regression coverage only while the demonstration domain exists.

## Repository gate

Run:

```powershell
.\scripts\Invoke-E2ETests.ps1
```

CI additionally validates:

- production Docker Compose interpolation;
- Bash deployment, rollback, backup, and restore syntax;
- Caddy TLS/proxy configuration;
- Prometheus scrape and alert rules plus Alertmanager routing configuration;
- backend and frontend image builds;
- HIGH and CRITICAL image vulnerabilities before publication.

## Staging gate

Deploy an immutable commit-SHA image through the protected `staging` GitHub Environment.

Required evidence:

- migration container completed successfully;
- public `/health/live`, `/health/ready`, and `/health/workers` return HTTP 200;
- automatic TLS certificate is valid for the configured domain;
- registration, email confirmation and login workflows pass;
- the current project/task regression workflows pass while the demo domain remains;
- the ProcureFlow request, approval, fulfillment and attachment workflow passes before `v1.0.0`;
- Grafana receives application and host data;
- all configured Prometheus alert rules are loaded and Alertmanager delivers a test notification;
- a backup is copied off host;
- the same backup is restored on staging or an isolated clone;
- manual rollback restores the previous image and passes readiness.

## Production decision

Do not promote when any of the following is true:

- the target runtime or framework is outside vendor support;
- staging differs from production in cookie, TLS, database, storage, or migration behavior;
- the latest backup has not passed checksum verification;
- no previous immutable image is available;
- required GitHub Environment approval or host-key pinning is missing;
- image scanning reports an unresolved HIGH or CRITICAL vulnerability.

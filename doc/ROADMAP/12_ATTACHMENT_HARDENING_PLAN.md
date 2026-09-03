# Attachment hardening plan

> [!NOTE]
> This plan records the hardening of attachments in the temporary `ProjectTasks`
> domain. The current operational contract is
> [Attachment Operations](../ATTACHMENT_OPERATIONS.md). PF5 reuses the verified
> storage and scanning mechanics behind new contracts owned by `PurchaseRequests`;
> it must not rename the existing domain types in place.

## Goal

Attachments must remain private, bounded and recoverable. A client-provided file
name, content type or size is metadata only and cannot be the security boundary.

The implementation remains incremental. Local storage is suitable for development,
while production storage and malware scanning are adapters selected for a concrete
deployment environment rather than dependencies added in advance.

## Current state

Implemented:

- authenticated upload, list, download and delete vertical slices;
- project membership and role checks for every attachment operation;
- 10 MiB file-size policy and an allowlist of extensions and media types;
- byte-level signatures for PDF, PNG and JPEG;
- required package entries for DOCX and XLSX;
- strict UTF-8 and control-character checks for TXT;
- comparison of declared and actual stream length;
- application-generated storage keys and rejection of traversal, arbitrary names and
  alternate data stream syntax;
- cleanup after metadata persistence failure and a durable cleanup queue for deletes;
- validated per-task count and total-byte quotas;
- PostgreSQL `FOR UPDATE` serialization for concurrent quota checks;
- count and byte quota concurrency tests against PostgreSQL;
- private S3-compatible storage with MinIO in Compose and an AWS-compatible adapter;
- synchronous fail-closed ClamAV scanning when malware scanning is required;
- provider inventory reconciliation, dependency health checks and operational alerts;
- coordinated database, object storage and Data Protection backup/restore procedures;
- browser upload, download, delete and viewer-authorization coverage;
- focused unit and API integration tests for the rules above.

Remaining product and target-environment gaps:

- replace `ProjectTask`-owned contracts and metadata with `PurchaseRequest`-owned
  contracts during PF5 without creating two active storage mechanisms;
- prove object storage, ClamAV availability, alerts and retention on the selected
  staging/production environment;
- copy backups off host and record a successful restore drill;
- introduce persisted quarantine states only if a future asynchronous scanner needs
  them; the current synchronous scanner accepts content before storage.

## Delivery stages

### Stage 1: Content and path validation

Status: **implemented**.

Keep format inspection in the use-case path so alternate transports cannot bypass it.
The HTTP request-size limit remains a coarse transport guard; the handler owns the
authoritative attachment policy.

### Stage 2: Configurable and atomic quotas

Status: **implemented for the current ProjectTasks domain**.

Introduce validated attachment options with conservative defaults:

- maximum file size: 10 MiB;
- maximum attachment count per task: 20;
- maximum total attachment bytes per task: 100 MiB.

Count and byte quotas are enforced in the database transaction that creates attachment
metadata. Concurrent uploads for the same task must serialize or use a concurrency
token so two requests cannot both pass a stale count. Return a stable validation or
conflict response and do not leave a binary behind when quota reservation fails.

Project-wide or per-user quotas should be added only after product requirements define
ownership, reset periods and administrator behavior. Rate limiting belongs at the HTTP
boundary and complements, but does not replace, persistent quotas.

### Stage 3: Production storage provider

Status: **repository implementation complete; target-environment proof pending**.

`IProjectTaskAttachmentStorage` remains the current application port. The local root
is configurable, and production startup rejects an unsupported ephemeral local
configuration.

The S3-compatible adapter uses private objects and supports AWS S3 or compatible
providers. Docker Compose uses a private MinIO bucket. Downloads continue through the
authorized API; deployment-specific encryption, credentials, backup and availability
must still be proven in the target environment.

The deployment runbook must include backup, restore, orphan reconciliation and a
migration procedure from local files to object storage.

### Stage 4: Quarantine, scanning and retention

Status: **synchronous scanning and cleanup contract implemented; asynchronous
quarantine remains conditional**.

The current ClamAV adapter scans synchronously before storage. A threat, timeout,
connection failure or malformed scanner response fails closed, so unscanned content
does not receive downloadable metadata. An asynchronous provider would require an
explicit lifecycle such as `PendingScan`, `Clean`, `Rejected` and `Deleted`; those
states are not added without that provider requirement.

Define retention separately for active attachments, rejected uploads and cleanup
messages. Scheduled cleanup must be idempotent and observable. Logs and audit events
must contain identifiers and outcomes, never file contents or signed download URLs.

### Stage 5: Verification and operations

Status: **continuous**.

Required coverage includes:

- mismatched extension, media type, signature and actual length;
- malformed and misleading Open XML archives;
- path traversal and invalid storage keys;
- exact file, count and total-byte boundaries;
- concurrent uploads near quota;
- owner, member, viewer and outsider authorization;
- storage timeout, metadata failure, retry and orphan cleanup;
- restart persistence, backup restore and browser-level critical paths.

## Definition of done

Repository-level hardening of the current attachment mechanism is complete when the
focused unit, API, PostgreSQL and browser tests pass. A production-readiness claim
additionally requires the selected storage to survive replacement, a target-environment
scanner and alert check, an off-host backup, and a recorded restore drill. ProcureFlow
product completeness separately requires the PF5 `PurchaseRequest` contract and PF6
removal of the legacy attachment model.
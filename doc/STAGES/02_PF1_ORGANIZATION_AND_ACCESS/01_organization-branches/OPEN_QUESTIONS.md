# Open questions: organization and branch management

- Status: No known blocking product question

The branch is completed according to the canonical PF1 document. Use this file
for a question found during maintenance or review. Do not silently change a
business rule here; record the question first and move a resolved decision to
the appropriate architecture or roadmap document.

## Review questions for later

| ID | Question | Why it matters | Status |
|---|---|---|---|
| OBR-001 | Should repeated archive requests be idempotent or return conflict? | Defines the public API contract | Verify against current API behavior |
| OBR-002 | Which exact permission policy protects branch administration? | Prevents authorization drift | Verify in API and tests |
| OBR-003 | Which historical aggregates may reference an archived branch? | Protects future foreign keys and deletion rules | Revisit when purchase requests are added |

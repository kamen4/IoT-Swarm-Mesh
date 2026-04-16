---
description: Load these rules for consistency checks, audit logging, and readiness gate closure across Implementation.
applyTo: "Implementation/**"
name: "Implementation Audit and Closure"
---

# Audit and closure instructions

## Audit trigger points

Run consistency checks after:

- adding or changing task IDs
- changing dependencies
- editing source pointers
- changing baseline parameter policy
- modifying readiness gate logic

## Audit modes

Read-only audit mode:

- report findings only;
- do not modify files;
- record summary in chat/report output;
- use for pre-run and monitoring checks.

Consistency-fix audit mode:

- fix resolvable reference and linkage issues;
- do not make scope or baseline changes automatically;
- record all fixes in Implementation/DevPlan/92-audit-log.md.

## Audit checklist

- all referenced files exist
- all dependency IDs resolve
- validation owner IDs exist
- risk mitigation IDs exist
- source pointers are still correct
- no contradictions with scope or baseline rules
- evidence links and paths follow Implementation/DevPlan/evidence-artifact-convention.md

## Audit log entry contract

Each audit entry should include:

- touched-file
- audit-timestamp
- pass-fail
- unsupported-claim-findings
- fix-link

## Readiness closure policy

Gate can be marked PASS only when objective evidence exists.

- traceability-pass: source mapping complete and checked
- terminology-pass: naming consistency verified
- baseline-pass: baseline lock evidence attached
- recheck-pass: audit log updated for latest changes
- consistency-pass: cross-file review complete

## Baseline external change detection

If new or modified baseline artifacts are detected under Protocol/_theoreme_ai_search during active execution:

- mark affected parameter and validation items PROVISIONAL;
- append detection entry to Implementation/DevPlan/92-audit-log.md;
- stop affected chain and request explicit baseline decision.

## Escalation rule

If uncertain or conflicting sources are found:

- mark affected items PROVISIONAL
- record required decision in Implementation/DevPlan/92-audit-log.md
- do not silently force a final decision

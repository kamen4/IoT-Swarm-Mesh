---
description: Load these rules when running consistency checks after non-trivial Implementation changes.
applyTo: "Implementation/**"
name: "Implementation Consistency Check Protocol"
---

# Consistency check protocol

## Trigger

Run this protocol after each non-trivial batch and before readiness gate updates.

## Required checks (run in parallel)

1. Spec internal consistency (no contradictory requirements across Spec files).
2. DevPlan task integrity (all task IDs exist, dependencies resolve, no cycles).
3. Validation ownership integrity (owner-task IDs exist and are active).
4. Risk linkage integrity (mitigation task IDs exist and map to observable signals).
5. Source pointer validity (all listed source paths resolve).
6. Readiness consistency (gate statuses and open issues match latest audit state).

## Evidence and output contract

Each run MUST produce:

- check timestamp (UTC)
- pass-fail per check
- findings list for failed checks
- touched files list
- remediation links

## Pass criteria

- PASS only if all checks pass, or failures are explicitly marked PROVISIONAL with linked decision records.
- FAIL if any unresolved conflict, undefined task ID, broken source pointer, or dependency cycle remains.

## Escalation

If the same failed check appears in 3 consecutive runs, stop automation and escalate with blocker summary in audit log.

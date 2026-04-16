# Authoring Rules

This document defines how to write and maintain Implementation/Spec files.

## Normative Language

Use these keywords consistently:

- MUST / MUST NOT for hard requirements.
- SHOULD / SHOULD NOT for strong recommendations.
- MAY for optional behavior.

## Terminology Rules

- Preserve protocol names exactly as in Protocol/_docs_v1.0/00-glossary.md.
- Preserve field names exactly: q_up, q_total, q_forward, originMac, dstMac, msgId, seq, prevHopMac.
- Preserve state names exactly: Pending, Verified, Connected.
- Preserve message names exactly as registry names (FIND, PONG, VERIFY, PROTO, PROTO_R, START, ACK, PULL, PULL_R, IO_GET, IO_GET_R, IO_SET, IO_SET_R, IO_EVENT, HELLO, WAKE, BEACON, DECAY, FRAG).

## Citation Format

For each requirement line:

- Source: path/to/file.md (section name if available)

Example:

- Requirement: Device MUST drop packet if ttl equals zero.
- Source: Protocol/_docs_v1.0/algorithms/02-message-envelope.md

## Provisional Marking

If behavior is not explicitly defined in sources, mark it:

- Status: PROVISIONAL
- Rationale: what is missing
- Required follow-up: add decision record in DevPlan/92-audit-log.md

## Baseline Parameter Rule

Defaults in documentation MUST match:

- Protocol/_theoreme_ai_search/try_3_baseline/candidate_sweep_requests/cand2_more_inertia.json

Changing defaults requires:

- a documented decision,
- updated source map,
- updated DevPlan validation and readiness checklist.

---
description: Load these instructions for any task under Implementation, including Spec and DevPlan authoring, restructuring, audits, and consistency updates.
applyTo: "Implementation/**"
name: "Implementation Global Instructions"
---

# Implementation global instructions

Use these instructions as the default policy for all work in Implementation.

## Purpose

- Keep Implementation as a documentation-first execution package for the protocol product.
- Keep Spec and DevPlan consistent, traceable, and audit-ready.

## Specialized instruction files

- For Spec authoring rules, also load:
  - Implementation/.github/instructions/spec-authoring.instructions.md
- For DevPlan authoring rules, also load:
  - Implementation/.github/instructions/devplan-authoring.instructions.md
- For Wave1 execution workflow, also load:
  - Implementation/.github/instructions/wave1-execution.instructions.md
- For audits and gate closure, also load:
  - Implementation/.github/instructions/audit-closure.instructions.md
- For consistency audit execution protocol, also load:
  - Implementation/.github/instructions/consistency-check-protocol.instructions.md

## Core principles

1. Documentation is the source of truth for Implementation work.
2. Spec and DevPlan must never contradict each other.
3. Prefer small, focused files over large mixed documents.
4. Apply minimal targeted changes and avoid unrelated refactors.
5. If behavior changes, update all affected planning and quality files in the same task.

## Documentation authority and synchronization

- Treat Implementation/Spec as normative behavior definition.
- Treat Implementation/DevPlan as executable task decomposition.
- When code-facing behavior is changed in Spec, update matching DevPlan tasks, dependencies, and validation ownership.
- When tasks are added, removed, or renumbered in DevPlan, update all references in:
  - Implementation/DevPlan/Wave1/03-integration/03-dependency-map.md
  - Implementation/DevPlan/90-validation-matrix.md
  - Implementation/DevPlan/91-risk-register.md
  - Implementation/DevPlan/99-readiness-checklist.md

## Documentation hierarchy rules

- Keep one navigation file for every meaningful planning scope:
  - Root scope: Implementation/Spec/00-index.md and Implementation/DevPlan/00-index.md
  - Nested scope: local _index.md or numbered index file where needed
- Every new folder must be reflected in the nearest parent index in the same change.
- Every moved, renamed, or deleted file must be reflected in affected index and dependency files in the same change.

## Traceability and source rules

- Every normative statement and task cluster must include source pointers.
- If no authoritative source exists, mark the item as PROVISIONAL and include:
  - Status: PROVISIONAL
  - Rationale: what is missing
  - Required follow-up: add decision record in Implementation/DevPlan/92-audit-log.md
- Do not silently invent protocol behavior.

## Terminology and naming rules

- Preserve protocol names and symbols exactly as documented in Protocol/_docs_v1.0.
- Preserve core fields exactly: q_up, q_total, q_forward, originMac, dstMac, msgId, seq, prevHopMac.
- Preserve lifecycle states exactly: Pending, Verified, Connected.
- Preserve message names exactly as registry values.

## DevPlan task rules

- Keep tasks at 0.5 to 1 day granularity.
- Keep task IDs unique and stable.
- Dependencies must reference existing task IDs only.
- Dependency graphs must remain acyclic.
- If adding tasks, update dependency chains in the dependency map in the same change.

## Baseline parameter policy

- Wave 1 default baseline is fixed to cand2_more_inertia.
- Parameter defaults must match Protocol/_theoreme_ai_search/try_3_baseline/candidate_sweep_requests/cand2_more_inertia.json.
- Any baseline change requires same-task updates to:
  - Implementation/DevPlan/Wave1/02-protocol/04-params-baseline.md
  - Implementation/DevPlan/90-validation-matrix.md
  - Implementation/DevPlan/99-readiness-checklist.md
  - Implementation/DevPlan/92-audit-log.md

## Baseline external change response

- If a new baseline candidate appears under Protocol/_theoreme_ai_search while a run is active, do not auto-switch baseline.
- Record detection event in Implementation/DevPlan/92-audit-log.md with path and timestamp.
- Mark affected parameter-validation items as PROVISIONAL until decision is approved.
- Stop affected task chain and escalate for explicit baseline decision.

## Cross-boundary coordination rules

- If Implementation/Spec changes protocol behavior that can affect theorem verification or convergence evaluation, add a coordination note in Implementation/DevPlan/92-audit-log.md.
- If changes impact theorem assumptions or simulation phase semantics, review alignment with:
  - Protocol/.github/instructions/theoreme_sim-global.instructions.md
  - Protocol/_docs_v1.0/math/theorem.md
  - Protocol/_docs_v1.0/mitigations/simulation-pipeline.md
- If changes impact baseline parameter interpretation for comparative simulation, review alignment with:
  - SimModel/SimModel/.github/instructions/global.instructions.md

## Quality and audit workflow

- Keep validation ownership and evidence requirements aligned.
- Keep risk mitigation task IDs resolvable.
- Record non-trivial edits and audit outcomes in Implementation/DevPlan/92-audit-log.md.
- For broad multi-file changes, use parallel subagents for consistency checks, then consolidate results.
- Keep all evidence paths and names compliant with Implementation/DevPlan/evidence-artifact-convention.md.

## Agent decision boundaries

Stop and escalate when any of these conditions is true:

- conflicting source pointers are found for the same normative statement;
- glossary or baseline source changed during active execution chain;
- more than 3 tasks in one chain are blocked by the same unresolved decision;
- required artifact or dependency is outside repository scope;
- repeated failed consistency check appears 3 times in a row.

## Structure and scope guardrails

- Keep Implementation focused on protocol product scope for Wave 1: device + gateway + hub.
- Do not introduce unrelated technologies, build systems, or runtime architecture into Implementation docs.
- Keep folder boundaries explicit and responsibilities separated by file.

## Character and formatting rules

- Use ASCII only.
- Replace non-ASCII punctuation and symbols with ASCII equivalents:
  - Use -- or - instead of em dash or en dash.
  - Use -> and <- instead of arrow symbols.
  - Use <= and >= instead of Unicode comparison symbols.
  - Use x instead of multiplication symbol.
  - Use O(n^2) instead of superscript notation.
- Keep Markdown compact, structured, and easy to scan.
- Use tables for task lists, validation matrices, and risk mappings.

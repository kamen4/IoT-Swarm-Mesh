---
description: Load these rules when editing Implementation/Spec files, including new requirements, refactors, and traceability updates.
applyTo: "Implementation/Spec/**"
name: "Implementation Spec Authoring"
---

# Spec authoring instructions

## Scope

Use this file for all normative documentation work in Implementation/Spec.

## Required section shape

Every Spec file should include, when applicable:

- Purpose or Objective
- Requirements
- Invariants or Constraints
- Source Pointers

## Requirement formatting

- Write concise normative statements.
- Use MUST, SHOULD, MAY consistently.
- Keep one idea per requirement line.

Recommended format:

- Requirement: <statement>
  - Source: <path>

## Source and traceability rules

- Every normative requirement must have at least one source.
- If no source exists, mark requirement as PROVISIONAL and include:
  - Status: PROVISIONAL
  - Rationale: <missing source or ambiguity>
  - Required follow-up: add decision record in Implementation/DevPlan/92-audit-log.md

## Cross-file update rules

When Spec behavior changes, update in the same task:

- Implementation/Spec/00-source-map.md
- Implementation/Spec/00-index.md if navigation changed
- related Integration or Quality Spec files affected by the change
- impacted DevPlan ownership in validation/risk/readiness files

## Terminology lock

Preserve canonical names exactly:

- q_up, q_total, q_forward
- originMac, dstMac, msgId, seq, prevHopMac
- Pending, Verified, Connected
- message registry names from protocol docs

## Prohibited actions

- Do not invent protocol behavior without PROVISIONAL marking.
- Do not change registry values or field names silently.
- Do not leave source pointers stale after edits.

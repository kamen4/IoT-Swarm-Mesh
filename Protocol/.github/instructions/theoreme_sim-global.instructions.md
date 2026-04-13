---
description: "Use when editing _theoreme_sim files, implementing theorem simulation logic, adjusting graph rendering, or updating mathematical checks (A5/A6/A7, Lemma 4.1/4.2/4.3)."
name: "_theoreme_sim Global Instructions"
applyTo: "_theoreme_sim/**"
---

# \_theoreme_sim Global Instructions

## Purpose

- Maintain \_theoreme_sim as a static, browser-only mathematical simulator for the DOWN charge-induced tree theorem.
- Keep the implementation spec-driven, modular, and easy to audit.

## Architecture Rules

- Keep the app static: HTML + CSS + ES modules only.
- Do not add bundlers, backend services, or runtime dependencies unless explicitly requested.
- Preserve small single-purpose modules and folder separation in src/.
- Keep imports as relative ES module imports with explicit .js extension.

## Mathematical Integrity

- Treat the simulator as a structural theorem verifier on a stable charge field.
- Preserve and verify checks for:
  - Assumptions: A5, A6, A7
  - Lemmas: 4.1 (strict increase), 4.2 (acyclic), 4.3 (reachability to gateway)
- Parent selection must enforce strict higher-charge parent relation.
- Any tie-break logic must not violate strict charge monotonicity.
- If theorem semantics change, update both code and simulator documentation in the same change.

## Visualization Rules

- Continue rendering the full graph plus induced parent tree.
- Keep charge-driven visual encoding:
  - Edge weight labels shown on links
  - Edge thickness and brightness mapped to weight/charge
  - Gateway and eligibility visually distinct
- Keep canvas rendering deterministic for the same seed/config.

## Documentation Rules

- Keep README.md present and accurate in every \_theoreme_sim folder.
- Every new code file must start with a short purpose comment.
- Update affected README files when adding/removing modules.

## Safety and Quality

- Avoid silent behavior changes in theorem-critical logic.
- After edits, run diagnostics and resolve relevant errors.
- Prefer minimal, targeted changes; avoid unrelated refactors.

# Product Scope (Wave 1)

## Objective

Define what is included in Wave 1 implementation and what is excluded.

## In Scope

- Device-side protocol behavior over ESP-NOW.
- Gateway behavior as bridge between mesh and HUB.
- HUB-side lifecycle and command/telemetry orchestration.
- Onboarding, identity verification, message authenticity, UP and DOWN routing.
- Fixed baseline parameter profile cand2_more_inertia.

## Out of Scope

- Multi-gateway operation (provisional exclusion for Wave 1).
- Mandatory SimModel parity implementation in this wave.
- New crypto protocols beyond documented SPAKE2 + HMAC model.
- New transport stacks outside documented architecture.

See also: ../05-quality/02-out-of-scope-wave1.md.

## Wave 1 System Boundary

- Device node
- Gateway device
- HUB services (as described in architecture reference)

## Source Pointers

- Protocol/_docs_v1.0/reference/overview.md
- Protocol/_docs_v1.0/reference/architecture.md
- Protocol/_docs_v1.0/algorithms/01-onboarding.md
- Protocol/_docs_v1.0/algorithms/03-up-routing.md
- Protocol/_docs_v1.0/algorithms/04-down-routing.md
- Protocol/_theoreme_ai_search/try_3_baseline/candidate_sweep_requests/cand2_more_inertia.json

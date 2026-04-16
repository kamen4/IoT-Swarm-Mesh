# Implementation Spec Index

This folder contains the normative implementation specification for Wave 1 of the protocol product.

## Wave 1 Decisions

- Language: English
- Scope: protocol product only (device + gateway + hub)
- Fixed baseline: cand2_more_inertia
- Policy: do not introduce technologies not present in repository docs

## Navigation

- 00-index.md - entry point
- 00-source-map.md - requirement-to-source traceability
- 00-authoring-rules.md - writing and consistency rules
- 01-scope/01-product-scope.md - scope boundary for Wave 1
- 01-scope/02-glossary-alignment.md - preserved terms and names
- 02-protocol-core/01-envelope.md - frame envelope and byte constraints
- 02-protocol-core/02-message-types.md - message registry and direction/auth
- 02-protocol-core/03-onboarding.md - onboarding lifecycle and SPAKE2 flow
- 02-protocol-core/03b-onboarding-spake2-profile.md - concrete SPAKE2 crypto and payload profile
- 02-protocol-core/04-identity-security.md - identity and authenticity model
- 02-protocol-core/05-up-routing.md - UP forwarding behavior
- 02-protocol-core/06-down-routing.md - DOWN tree behavior
- 02-protocol-core/07-charge-decay.md - charge update and decay behavior
- 03-system/01-gateway-contract.md - gateway responsibilities
- 03-system/01b-gateway-uart-frame-spec.md - UART framing/checksum contract
- 03-system/02-hub-contract.md - hub responsibilities
- 03-system/03-rbac-and-users.md - role model requirements
- 03-system/04-persistence.md - persistence requirements
- 03-system/05-hub-service-contracts.md - full-product hub service contracts
- 04-integration/01-end-to-end-handshake.md - full onboarding integration contract
- 04-integration/02-forwarding-contracts.md - forwarding and delivery contracts
- 04-integration/03-error-handling-policy.md - error classification and handling policy
- 05-quality/01-validation-strategy.md - validation gates
- 05-quality/02-out-of-scope-wave1.md - explicit exclusions for Wave 1
- 06-full-product/00-index.md - full-product section navigation
- 06-full-product/01-full-product-spec.md - full product target specification

## Source Pointers

- Protocol/_docs_v1.0/00-glossary.md
- Protocol/_docs_v1.0/algorithms/01-onboarding.md
- Protocol/_docs_v1.0/algorithms/02-message-envelope.md
- Protocol/_docs_v1.0/algorithms/03-up-routing.md
- Protocol/_docs_v1.0/algorithms/04-down-routing.md
- Protocol/_docs_v1.0/algorithms/05-identity-security.md
- Protocol/_docs_v1.0/reference/overview.md
- Protocol/_docs_v1.0/reference/architecture.md
- Protocol/_docs_v1.0/reference/protocol.md
- Protocol/_docs_v1.0/reference/byte-sizes.md
- Protocol/_docs_v1.0/reference/users.md
- Protocol/_docs_v1.0/mitigations/corner-cases.md
- Protocol/_docs_v1.0/mitigations/convergence-tuning.md
- Protocol/_docs_v1.0/mitigations/simulation-pipeline.md
- Protocol/_docs_v1.0/math/model.md
- Protocol/_docs_v1.0/math/theorem.md
- Protocol/_theoreme_ai_search/try_3_baseline/candidate_sweep_requests/cand2_more_inertia.json
- Protocol/_theoreme_ai_conclusion/report.md

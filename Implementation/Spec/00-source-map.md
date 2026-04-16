# Source Map

This file maps each specification artifact to authoritative upstream documents.

## Mapping Table

| Spec File | Primary Sources |
| --- | --- |
| 01-scope/01-product-scope.md | Protocol/_docs_v1.0/reference/overview.md, Protocol/_docs_v1.0/reference/architecture.md |
| 01-scope/02-glossary-alignment.md | Protocol/_docs_v1.0/00-glossary.md |
| 02-protocol-core/01-envelope.md | Protocol/_docs_v1.0/algorithms/02-message-envelope.md, Protocol/_docs_v1.0/reference/protocol.md, Protocol/_docs_v1.0/reference/byte-sizes.md |
| 02-protocol-core/02-message-types.md | Protocol/_docs_v1.0/reference/protocol.md |
| 02-protocol-core/03-onboarding.md | Protocol/_docs_v1.0/algorithms/01-onboarding.md |
| 02-protocol-core/03b-onboarding-spake2-profile.md | Protocol/_docs_v1.0/algorithms/01-onboarding.md, Protocol/_docs_v1.0/reference/protocol.md, Protocol/_docs_v1.0/algorithms/05-identity-security.md |
| 02-protocol-core/04-identity-security.md | Protocol/_docs_v1.0/algorithms/05-identity-security.md |
| 02-protocol-core/05-up-routing.md | Protocol/_docs_v1.0/algorithms/03-up-routing.md |
| 02-protocol-core/06-down-routing.md | Protocol/_docs_v1.0/algorithms/04-down-routing.md, Protocol/_docs_v1.0/math/theorem.md |
| 02-protocol-core/07-charge-decay.md | Protocol/_docs_v1.0/algorithms/03-up-routing.md, Protocol/_docs_v1.0/mitigations/convergence-tuning.md |
| 03-system/01-gateway-contract.md | Protocol/_docs_v1.0/reference/architecture.md |
| 03-system/01b-gateway-uart-frame-spec.md | Protocol/_docs_v1.0/reference/architecture.md, Protocol/_docs_v1.0/mitigations/corner-cases.md, Protocol/_docs_v1.0/algorithms/02-message-envelope.md |
| 03-system/02-hub-contract.md | Protocol/_docs_v1.0/reference/architecture.md |
| 03-system/03-rbac-and-users.md | Protocol/_docs_v1.0/reference/users.md |
| 03-system/04-persistence.md | Protocol/_docs_v1.0/reference/architecture.md, Protocol/_docs_v1.0/algorithms/01-onboarding.md, Protocol/_docs_v1.0/algorithms/05-identity-security.md |
| 03-system/05-hub-service-contracts.md | Protocol/_docs_v1.0/reference/architecture.md, Protocol/_docs_v1.0/reference/users.md |
| 04-integration/01-end-to-end-handshake.md | Protocol/_docs_v1.0/algorithms/01-onboarding.md, Protocol/_docs_v1.0/reference/protocol.md |
| 04-integration/02-forwarding-contracts.md | Protocol/_docs_v1.0/algorithms/03-up-routing.md, Protocol/_docs_v1.0/algorithms/04-down-routing.md |
| 04-integration/03-error-handling-policy.md | Protocol/_docs_v1.0/mitigations/corner-cases.md, Protocol/_docs_v1.0/reference/protocol.md |
| 05-quality/01-validation-strategy.md | Protocol/_docs_v1.0/mitigations/simulation-pipeline.md, Protocol/_docs_v1.0/math/theorem.md, Protocol/_theoreme_ai_conclusion/report.md |
| 05-quality/02-out-of-scope-wave1.md | Protocol/_docs_v1.0/reference/architecture.md, Protocol/_docs_v1.0/mitigations/corner-cases.md, Implementation/Spec/01-scope/01-product-scope.md |
| 06-full-product/00-index.md | Protocol/_docs_v1.0/reference/overview.md, Protocol/_docs_v1.0/reference/architecture.md, Protocol/_docs_v1.0/reference/protocol.md |
| 06-full-product/01-full-product-spec.md | Protocol/_docs_v1.0/reference/overview.md, Protocol/_docs_v1.0/reference/architecture.md, Protocol/_docs_v1.0/reference/protocol.md, Protocol/_docs_v1.0/reference/users.md, Protocol/_docs_v1.0/algorithms/01-onboarding.md, Protocol/_docs_v1.0/algorithms/05-identity-security.md, Implementation/Spec/03-system/05-hub-service-contracts.md |

## Traceability Rules

- Every normative statement must reference at least one source above.
- If source coverage is missing, mark statement as provisional and add a follow-up item in DevPlan audit log.
- No source means no normative requirement.

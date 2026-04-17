# SPAKE2 Interoperability Decision Pack

## Purpose

Close onboarding interoperability decision required for server and device library alignment.

## Decision scope

- SPAKE2 profile details required for compatible implementations.
- Validation artifact requirements for interoperability evidence.

## Decision template

1. Profile definition
- Selected profile reference:
- Required parameter set:
- Hash binding requirements:

2. Message-level binding
- VERIFY step field constraints:
- Validation failure behavior:

3. Interoperability evidence
- Required cross-implementation checks:
- Required test-vector set reference:

4. Operational constraints
- Key-material handling boundaries:
- Re-onboarding behavior on verification failure:

## Acceptance criteria

- Profile is approved and published.
- Profile constraints are reflected in:
  - Implementation/Spec/03-protocol/onboarding.md
  - Implementation/Spec/06-device-library/onboarding-and-keys-spec.md
  - Implementation/Spec/04-server/spec.md
- Foundation WP-04 marks D-02 as closed with evidence.

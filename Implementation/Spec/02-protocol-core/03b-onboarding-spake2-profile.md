# Onboarding SPAKE2 Crypto Profile (Wave 1)

## Objective

Define the concrete SPAKE2 profile needed for interoperable server-device onboarding implementation.

## Profile Decisions

- Requirement: The onboarding handshake MUST use SPAKE2 over NIST P-256.
  - Source: Protocol/_docs_v1.0/reference/protocol.md (VERIFY step payload requires encoded group elements defined by crypto profile)
- Requirement: SPAKE2 group elements T_d and T_s MUST use SEC1 compressed point encoding with fixed length 33 bytes.
  - Source: Protocol/_docs_v1.0/reference/protocol.md
- Requirement: The onboarding password input w MUST be derived from SHA256(CONNECTION_KEY) interpreted as an unsigned integer reduced modulo curve order n.
  - Source: Protocol/_docs_v1.0/algorithms/01-onboarding.md
- Requirement: V_s and V_d MUST remain Trunc16(HMAC-SHA256(S_PASSWORD, label)) with labels SERVER_OK and DEVICE_OK exactly.
  - Source: Protocol/_docs_v1.0/algorithms/01-onboarding.md

## VERIFY Step Payload Profile

- Requirement: VERIFY step 1 data MUST be empty.
  - Source: Protocol/_docs_v1.0/reference/protocol.md
- Requirement: VERIFY step 2 data MUST be exactly 33 bytes (T_d compressed point).
  - Source: Protocol/_docs_v1.0/reference/protocol.md
- Requirement: VERIFY step 3 data MUST be exactly 49 bytes (T_s 33 bytes + V_s 16 bytes).
  - Source: Protocol/_docs_v1.0/reference/protocol.md
- Requirement: VERIFY step 4 data MUST be exactly 16 bytes (V_d).
  - Source: Protocol/_docs_v1.0/reference/protocol.md

## Validation Rules

- Requirement: Endpoints MUST reject VERIFY step payloads with unexpected lengths.
  - Source: Protocol/_docs_v1.0/reference/protocol.md
- Requirement: Endpoints MUST reject points that are not valid curve points or represent the point at infinity.
  - Source: Protocol/_docs_v1.0/algorithms/01-onboarding.md
- Requirement: Endpoint comparison of V_s and V_d MUST use constant-time byte comparison.
  - Source: Protocol/_docs_v1.0/algorithms/05-identity-security.md

## Persistence and Transition Rules

- Requirement: S_PASSWORD MUST be persisted only after successful VERIFY step 4 validation.
  - Source: Protocol/_docs_v1.0/algorithms/01-onboarding.md
- Requirement: Failed VERIFY MUST not transition lifecycle state to Connected.
  - Source: Protocol/_docs_v1.0/algorithms/01-onboarding.md

## Source Pointers

- Protocol/_docs_v1.0/algorithms/01-onboarding.md
- Protocol/_docs_v1.0/reference/protocol.md
- Protocol/_docs_v1.0/algorithms/05-identity-security.md
- Implementation/Spec/02-protocol-core/03-onboarding.md

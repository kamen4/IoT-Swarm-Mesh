# Onboarding Specification

## Objective

Define the onboarding lifecycle and required transitions from unregistered device to connected state.

## Lifecycle States

- Pending: device registered, not yet verified.
- Verified: SPAKE2 completed, S_PASSWORD established.
- Connected: protocol schema fetched and START accepted.

## Required Flow

1. User submits connection string to system.
2. Server sends FIND toward target MAC.
3. Device replies PONG.
4. VERIFY handshake completes (SPAKE2 steps).
5. Server requests protocol schema via PROTO.
6. Device returns PROTO_R.
7. Server issues START and device becomes Connected.

## Security Constraints

- CONNECTION_KEY is never transmitted directly.
- S_PASSWORD is derived during handshake and used for HMAC-authenticated flow.
- SPAKE2 curve, encoding, payload lengths, and validation gates MUST follow Implementation/Spec/02-protocol-core/03b-onboarding-spake2-profile.md.

## Retry and Timeout Policy

- Retry behavior is implementation-specific but MUST preserve idempotent state transitions.
- Failed VERIFY MUST keep device out of Connected state.

## Source Pointers

- Protocol/_docs_v1.0/algorithms/01-onboarding.md
- Implementation/Spec/02-protocol-core/03b-onboarding-spake2-profile.md

# Identity and Security

## Identity Model

- Device identity starts from QR bootstrap material.
- CONNECTION_KEY is device-local secret and is not transmitted directly.
- S_PASSWORD is derived during VERIFY/SPAKE2 onboarding.

## Authenticity Model

- End-to-end authenticity uses HMAC-SHA256 with truncation.
- TAG for authenticated frames is derived from SECURE_HEADER and PAYLOAD.
- Forwarding metadata in ROUTING_HEADER is mutable and outside HMAC boundary.

## Trust Boundary

- Mesh transport is treated as untrusted.
- Intermediate forwarders are not identity authorities.
- Server-side state is the source of truth for verified device identity.

## Control Message Policy

- Mesh-control hints can be unauthenticated as specified.
- Processing of unauthenticated control messages MUST include TTL and dedup safeguards.

## Source Pointers

- Protocol/_docs_v1.0/algorithms/05-identity-security.md

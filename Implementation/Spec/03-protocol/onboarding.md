# Onboarding Contract

## Sequence

1. Device exposes CONNECTION_STRING in QR flow.
2. User sends CONNECTION_STRING to server via Telegram flow.
3. Server starts discovery using FIND.
4. Device replies PONG.
5. Server and device run SPAKE2 sequence via VERIFY.
6. Derived per-device secret S_PASSWORD is established.
7. Server requests protocol definition via PROTO.
8. Device returns PROTO_R.
9. Server sends START and marks device connected.

## Lifecycle states

- Pending
- Verified
- Connected

## Required guarantees

- Device identity binding to onboarding artifact.
- Per-device secret derivation and persistence policy.
- Replay protection via seq and dedup behavior in message handling.

## OPEN DECISIONS

- SPAKE2 concrete profile details (curve/parameters/reference vectors) if not fixed by source set.
- Exact QR payload canonical encoding details.
- Decision-closure document:
	- Implementation/Spec/03-protocol/spake2-interoperability-decision-pack.md

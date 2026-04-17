# Server State And Data

## Device lifecycle state

- Pending
- Verified
- Connected
- Optional implementation states:
  - Revoked
  - Offline
  - Re-onboarding

## Required data domains

- Device identity and onboarding artifacts.
- Per-device protocol metadata from PROTO_R.
- User and role assignments.
- Command history and acknowledgements.
- Telemetry and event stream references.

## Minimal logical schema (technology-agnostic)

- users:
  - User identity attributes.
  - Role assignment attributes.
- devices:
  - Device identity, lifecycle state, and protocol metadata pointer.
- onboarding_sessions:
  - Onboarding progress checkpoints and verification artifacts.
- commands:
  - Outbound command intents, retries, and acknowledgement status.
- telemetry_events:
  - Inbound measurement or event payload references and timestamps.

## Data constraints

- Preserve message correlation keys (origin, msgId, seq context).
- Preserve audit trail for onboarding and admin actions.
- OPEN DECISION: exact schema and retention definitions.

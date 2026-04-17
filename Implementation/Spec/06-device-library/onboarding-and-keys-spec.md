# Device Library Onboarding And Keys

## Onboarding requirements

- Generate and expose onboarding artifact for user transfer flow.
- Respond correctly to FIND/PONG and VERIFY sequencing.
- Complete PROTO_R and START readiness transition.

## Key handling requirements

- Protect CONNECTION_KEY input material lifecycle.
- Derive and store per-device S_PASSWORD.
- Prevent accidental key leakage in logs and diagnostics.

## OPEN DECISIONS

- Device-side key persistence hardening profile by hardware class.
- Rotation/re-onboarding automation boundaries.

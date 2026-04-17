# Device Library WP-02 Onboarding And Security

## Goal

Implement onboarding and end-to-end authentication behavior in library surface.

## Tasks

T1. Onboarding flow implementation model
- T1.1 Define onboarding artifact generation behavior.
- T1.2 Define FIND/PONG handling behavior.
- T1.3 Define VERIFY sequence handling behavior.

T2. Key lifecycle handling
- T2.1 Define per-device secret derivation handoff.
- T2.2 Define key storage access boundaries.
- T2.3 Define key purge/re-onboarding behavior.

T3. Tag handling
- T3.1 Define tag generation path for authenticated messages.
- T3.2 Define authenticated message validation behavior where required.
- T3.3 Define untrusted hint message handling boundaries.

## Deliverables

- Onboarding behavior specification.
- Key lifecycle and tag-handling specification.

## Acceptance criteria

- Library onboarding and security behavior is fully aligned with protocol contracts.

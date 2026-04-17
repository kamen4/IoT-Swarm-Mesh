# Context Boundaries

## External interfaces

- Telegram Bot interface for user commands.
- UART/Serial link to gateway.
- ESP-NOW radio medium for mesh forwarding.

## Internal system domains

- Identity/onboarding domain.
- Command and control domain.
- Telemetry/event domain.
- Routing state domain.

## Boundary constraints

- No requirement for internet access.
- Reliability is bounded by RF environment and documented mitigations.
- Confidentiality is not guaranteed by the documented v1 protocol model.

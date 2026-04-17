# Scope And Limits

## In scope

- Implementation specification for:
  - Server
  - Gateway device
  - Device library
- Protocol behavior binding to documented v1 rules.
- Development-facing requirement decomposition for implementation planning.

## Out of scope

- Simulator implementation details as normative production requirements.
- New protocol features not documented in Protocol/_docs_v1.0.
- Stack lock-in not declared in source docs.

## Constraints

- On-premises architecture.
- ESP-NOW mesh transport assumptions.
- Gateway to host bridge over UART/Serial.
- Security model is per-device secret with end-to-end HMAC.
- Mesh-control messages are untrusted hints (TAG=0 path).

## Open-decision policy

When source documentation does not define a value or algorithm detail:
- Mark item as OPEN DECISION.
- Do not introduce undocumented defaults as mandatory.

# UART Frame Decision Pack

## Purpose

Close blocking server-gateway interoperability decision before implementation starts.

## Decision scope

- Frame boundary model.
- Length field definition.
- Frame integrity field definition.
- Stream resynchronization strategy.

## Decision template

1. Frame structure
- Field order:
- Field sizes:
- Allowed payload range:

2. Integrity profile
- Integrity algorithm:
- Validation failure behavior:

3. Resynchronization profile
- Trigger conditions:
- Recovery sequence:
- Maximum tolerated invalid frames before escalation:

4. Compatibility checks
- Server parser compatibility evidence:
- Gateway parser compatibility evidence:

## Acceptance criteria

- Decision is documented and approved.
- Decision is reflected in:
  - Implementation/Spec/05-gateway/uart-and-buffering-spec.md
  - Implementation/Spec/04-server/integration-spec.md
- Decision is referenced in Foundation WP-04 closure report.

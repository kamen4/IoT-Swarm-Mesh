# Open Questions And Risk Items

## Protocol detail gaps

- OPEN DECISION: exact SPAKE2 profile details for implementation interoperability.
- OPEN DECISION: UART framing and integrity strategy.
- OPEN DECISION: exact timing values for beacon/decay/parent expiry windows.

## Product-level gaps

- OPEN DECISION: explicit SLA targets for latency and reliability.
- OPEN DECISION: confidentiality requirements beyond documented authenticity model.
- OPEN DECISION: production capacity targets by device count and traffic profile.

## Delivery risks

- Parameter profile may not transfer 1:1 from simulation to field RF conditions.
- Misconfigured queue policy can degrade control path under telemetry bursts.
- Incomplete role policy mapping can create authorization ambiguity.

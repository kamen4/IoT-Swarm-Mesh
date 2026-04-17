# Gateway Routing And Resilience

## Routing responsibilities

- Origin point for DOWN propagation.
- Participation in UP path reception and server delivery.
- Proper handling of BEACON and DECAY related control behavior.

## Resilience requirements

- Safe startup sequence for mesh participation.
- Dedup and ttl safeguards for storm containment.
- Controlled handling when parent/child relation updates occur across network.

## Startup sequence contract

1. Initialize local runtime state and bounded caches.
2. Establish host bridge readiness on UART channel.
3. Enter mesh receive/forward mode with ttl and dedup safeguards active.
4. Start control-message participation according to configured policy.

## OPEN DECISIONS

- Bootstrapping cadence for control broadcasts.
- Recovery policy after transient UART disconnect.

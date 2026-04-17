# Component Contracts

## Server contract

- Must implement onboarding sequence and device lifecycle states.
- Must issue protocol messages according to message registry.
- Must validate end-to-end authenticated messages after onboarding.
- Must manage pending command delivery for sleepy devices.

## Gateway contract

- Must bridge UART <-> ESP-NOW frames.
- Must apply routing header updates per hop.
- Must maintain dedup behavior and TTL handling.
- Must execute DOWN forwarding as children-only when eligible.

## Device library contract

- Must expose APIs for onboarding, message handling, and interaction protocol.
- Must maintain per-device keying material lifecycle.
- Must implement UP and DOWN handling rules for endpoint runtime.

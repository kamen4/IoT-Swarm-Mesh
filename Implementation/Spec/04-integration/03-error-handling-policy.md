# Error Handling Policy

## Purpose

Define protocol-level error handling policy consistent with reference behavior and mitigations.

## Error Classes

- Envelope and framing errors
- Authentication and verification failures
- Routing guard failures (ttl, dedup, ineligible forwarding)
- Convergence and topology instability signals

## Handling Principles

- Reject malformed or non-compliant frames safely.
- Preserve node stability over delivery aggressiveness under overload.
- Prefer bounded retries and explicit state-safe fallback.

## Wave 1 Policy

- Unknown message-type handling is implementation-defined and MUST be documented by implementation profile.
- All traffic remains subject to dedup and ttl guards. Control-message abuse SHOULD be constrained by rate limits and type filters.

## Recovery Mapping

| Error Class | Recovery Action |
| --- | --- |
| Envelope and framing errors | Drop frame, keep node running, continue processing next frame |
| Authentication and verification failures | Reject message or onboarding step, keep lifecycle below Connected until successful VERIFY |
| Routing guard failures | Drop frame on ttl/dedup/ineligible checks and continue routing loop |
| Convergence/topology instability | Apply documented mitigations (hysteresis, decay, conservative switching) |

## Error Code Note

- Protocol-level non-zero error semantics are implementation-defined and MUST be documented per implementation profile.

## Source Pointers

- Protocol/_docs_v1.0/mitigations/corner-cases.md
- Protocol/_docs_v1.0/reference/protocol.md

# Risk Register

## Risk format

- Risk ID
- Description
- Trigger
- Impact
- Mitigation task
- Owner
- Review cadence

## Initial program risks

R-01
- Description: unresolved UART frame contract blocks server-gateway interoperability.
- Trigger: phase 2 or 3 implementation starts without agreed frame definition.
- Impact: rework and integration delay.
- Mitigation task: close contract in Foundation WP-04.

R-02
- Description: unresolved SPAKE2 profile detail causes interop mismatch.
- Trigger: incompatible implementations across server and device library.
- Impact: onboarding failure.
- Mitigation task: close onboarding profile decision in Foundation WP-04.

R-03
- Description: parameter policy mismatch between streams causes instability.
- Trigger: independent parameter defaults in server/gateway/library.
- Impact: oscillation and delivery degradation.
- Mitigation task: central parameter governance in Integration WP-02.

R-04
- Description: role policy ambiguity leads to authorization defects.
- Trigger: command permission matrix is not finalized.
- Impact: security and governance violation.
- Mitigation task: explicit role matrix in Server WP-04.

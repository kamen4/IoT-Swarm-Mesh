# verification

Assumption and theorem verification utilities.

## Files

- assumptionChecks.js - Checks A5/A6/A7 from theorem assumptions.
- theoremChecks.js - Lemma checks and final theorem status evaluation.
- oscillationDetector.js - Rolling parent-flap and oscillation metrics.

## Notes

- Theorem status may be PENDING while the eligible non-root set is still empty.
- PASS/FAIL is evaluated after at least one eligible non-root node appears.

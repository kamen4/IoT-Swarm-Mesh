# optimization

Adaptive search utilities for research batch tuning.

## Files

- candidateSelection.js - Candidate objective and comparison helpers; objective now combines score, stable ratio, tail-eligibility health, flapping penalty, and cross-seed variance penalty, plus plateau-aware step-size reheat logic.

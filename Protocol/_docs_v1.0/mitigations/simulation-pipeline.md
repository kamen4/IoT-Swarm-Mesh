# Simulation Pipeline (Python Batch Model)

This document defines the canonical per-round phase order used in the Python simulator (`_theoreme_sim_py`).

## Canonical round order

1. `run_down_round_phase`
   - Root-origin DOWN propagation.
   - Tracks duplicates, deliveries, reached count, coverage.

2. `run_up_round_phase`
   - Greedy UP forwarding toward gateway.
   - Updates charges and neighbor estimates on successful delivery.

3. `propagate_neighbor_charges_round`
   - Pairwise estimate propagation over edges.
   - Uses delivery probability and link usage tracking.

4. `apply_charge_spread_round`
   - Charge blending toward best estimate.
   - Formula: `q_next = q_current + (target - q_current) * chargeSpreadFactor`.

5. `finalize_link_strength_round`
   - Updates effective link quality from observed usage.

6. `refresh_eligibility`
   - Recomputes `eligible = (q_total >= qForward)`.

7. `rebuild_tree`
   - Parent selection with strict-higher-charge condition and hysteresis.

8. `maybe_run_decay_phase`
   - Optional decay on configured interval.
   - If triggered, eligibility and tree are recomputed immediately.

9. `update_oscillation_report`
   - Tracks parent changes, flapping nodes, max flips.

10. `evaluate_theorem`
    - Runs A5/A6/A7 and Lemma 4.1/4.2/4.3 checks.

## Why order matters

- Tree rebuild must happen after charge propagation/spread and eligibility refresh.
- Decay can change eligibility and parent choice, so decay-triggered rounds run another rebuild.
- Theorem checks must run last to inspect final round state.

## Verification-state semantics

- `pending`: theorem not evaluated yet for non-trivial eligible set.
- `pass`: all assumptions + lemmas + spanning condition pass.
- `fail`: any non-pending violation.

## Practical note

When comparing implementations, matching this exact order is required for parity of convergence curves, parent-flap behavior, and theorem pass timelines.

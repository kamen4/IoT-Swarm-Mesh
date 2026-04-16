# Charge and Decay Specification

## Objective

Define how charge values are accumulated, propagated, and attenuated over time.

## Charge Values

- q_up reflects UP routing attractiveness.
- q_total reflects aggregate routing centrality used by DOWN tree logic.

## Update Behavior

- Charge updates occur as part of forwarding and periodic propagation rounds.
- Local values are smoothed by spread factors to reduce instability.

## Decay Behavior

- DECAY epochs apply attenuation to reduce stale dominance.
- Decay cadence and percentage are controlled by baseline parameters.

## Multi-hop Charge Aggregation

- Requirement: Forwarded UP traffic MUST increment local q_up and q_total before advertising charge to next hop.
- Requirement: Neighbor charge observations MUST be merged monotonically per routing direction.
- Requirement: Multi-hop aggregation MUST preserve deterministic update order within each runtime cycle.

## Parameter Validity Ranges

Wave 1 implementations MUST enforce these bounds at load and runtime validation:

| Parameter | Min | Max |
| --- | --- | --- |
| qForward | 20 | 1800 |
| deliveryProbability | 0.05 | 1.0 |
| penaltyLambda | 0 | 250 |
| switchHysteresis | 0 | 260 |
| switchHysteresisRatio | 0.00 | 0.40 |
| rootSourceCharge | 250 | 3000 |
| chargeDropPerHop | 5 | 420 |
| chargeSpreadFactor | 0.02 | 1.0 |
| decayIntervalSteps | 0 | 2000 |
| decayPercent | 0.00 | 0.80 |
| linkMemory | 0.600 | 0.999 |
| linkLearningRate | 0.01 | 2.00 |
| linkBonusMax | 0 | 240 |

## Baseline Defaults (Wave 1)

- decayIntervalSteps = 91
- decayPercent = 0.22
- chargeSpreadFactor = 0.08

Full baseline values are defined in DevPlan parameter file.

## Source Pointers

- Protocol/_docs_v1.0/algorithms/03-up-routing.md
- Protocol/_docs_v1.0/mitigations/convergence-tuning.md

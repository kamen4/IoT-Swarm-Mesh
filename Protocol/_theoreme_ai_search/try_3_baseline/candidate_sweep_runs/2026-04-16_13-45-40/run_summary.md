# Batch Run Summary

## Run

- Started: 2026-04-16T13:45:40.851608
- Finished: 2026-04-16T13:47:21.701274
- DurationSec: 100.85
- Topologies: 40
- TotalRuns: 800
- OptimizationIterations: 0
- SeedStart: 42
- SeedCount: 10
- RoundsPerCheck: 300
- ParallelWorkers: 10

## Best Recommendation

- Network: N=34, R=280
- Verdict: STABLE
- AvgScore: 99.84
- StableRatio: 1.000
- BestSeed: 42

## Axiom and Theorem Activation by Network

| Network | FirstAssumptions | FirstTheorem | FirstAllChecks | SustainedAssumptions | SustainedTheorem | SustainedAllChecks |
| --- | --- | --- | --- | --- | --- | --- |
| 10x60 | 2 | 2 | 2 | 2 | 2 | 2 |
| 10x90 | 2 | 2 | 2 | 2 | 2 | 2 |
| 10x120 | 2 | 2 | 2 | 2 | 2 | 2 |
| 10x160 | 2 | 2 | 2 | 2 | 2 | 2 |
| 14x70 | 2 | 2 | 2 | 2 | 2 | 2 |
| 14x100 | 2 | 2 | 2 | 2 | 2 | 2 |
| 14x140 | 2 | 2 | 2 | 2 | 2 | 2 |
| 14x180 | 2 | 2 | 2 | 101 | 101 | 101 |
| 18x80 | 2 | 2 | 2 | 2 | 2 | 2 |
| 18x120 | 2 | 2 | 2 | 201 | 201 | 201 |
| 18x160 | 2 | 2 | 2 | 2 | 2 | 2 |
| 18x210 | 2 | 2 | 2 | 2 | 2 | 2 |
| 24x100 | 2 | 2 | 2 | 2 | 2 | 2 |
| 24x140 | 2 | 2 | 2 | 2 | 2 | 2 |
| 24x190 | 2 | 2 | 2 | 2 | 2 | 2 |
| 24x240 | 2 | 2 | 2 | 2 | 2 | 2 |
| 34x120 | 2 | 2 | 2 | 2 | 2 | 2 |
| 34x170 | 2 | 2 | 2 | 2 | 2 | 2 |
| 34x220 | 2 | 2 | 2 | 2 | 2 | 2 |
| 34x280 | 2 | 2 | 2 | 2 | 2 | 2 |
| 48x150 | 2 | 2 | 2 | 2 | 2 | 2 |
| 48x210 | 2 | 2 | 2 | 2 | 2 | 2 |
| 48x270 | 2 | 2 | 2 | 2 | 2 | 2 |
| 48x330 | 2 | 2 | 2 | 2 | 2 | 2 |
| 64x170 | 2 | 2 | 2 | 2 | 2 | 2 |
| 64x230 | 2 | 2 | 2 | 2 | 2 | 2 |
| 64x290 | 2 | 2 | 2 | 2 | 2 | 2 |
| 64x350 | 2 | 2 | 2 | 2 | 2 | 2 |
| 80x200 | 2 | 2 | 2 | 2 | 2 | 2 |
| 80x260 | 2 | 2 | 2 | 2 | 2 | 2 |
| 80x320 | 2 | 2 | 2 | 2 | 2 | 2 |
| 80x380 | 2 | 2 | 2 | 2 | 2 | 2 |
| 96x220 | 2 | 2 | 2 | 2 | 2 | 2 |
| 96x280 | 2 | 2 | 2 | 2 | 2 | 2 |
| 96x340 | 2 | 2 | 2 | 2 | 2 | 2 |
| 96x400 | 2 | 2 | 2 | 2 | 2 | 2 |
| 120x250 | 2 | 2 | 2 | 2 | 2 | 2 |
| 120x320 | 2 | 2 | 2 | 2 | 2 | 2 |
| 120x390 | 2 | 2 | 2 | 2 | 2 | 2 |
| 120x460 | 2 | 2 | 2 | 2 | 2 | 2 |

## Best Network Check Activation Detail

| Check | FirstPassRound | SustainedFromRound | PassRatePercent |
| --- | --- | --- | --- |
| Assumptions | 2 | 2 | 99.67 |
| Theorem | 2 | 2 | 99.67 |
| A5 | 2 | 2 | 99.67 |
| A6 | 2 | 2 | 99.67 |
| A7 | 2 | 2 | 99.67 |
| Lemma41 | 2 | 2 | 99.67 |
| Lemma42 | 2 | 2 | 99.67 |
| Lemma43 | 2 | 2 | 99.67 |

## Input Request

```json
{
  "baseConfig": {
    "nodeCount": 48,
    "linkRadius": 210,
    "seed": 42,
    "maxRounds": 320,
    "qForward": 430,
    "deliveryProbability": 0.22,
    "rootSourceCharge": 1808,
    "penaltyLambda": 68,
    "switchHysteresis": 38,
    "switchHysteresisRatio": 0.07,
    "chargeDropPerHop": 94,
    "chargeSpreadFactor": 0.09,
    "decayIntervalSteps": 100,
    "decayPercent": 0.2,
    "linkMemory": 0.872,
    "linkLearningRate": 0.5,
    "linkBonusMax": 50
  },
  "seedCount": 10,
  "optimizationIterations": 0,
  "roundsPerCheck": 300,
  "matrixText": "10x60,10x90,10x120,10x160,14x70,14x100,14x140,14x180,18x80,18x120,18x160,18x210,24x100,24x140,24x190,24x240,34x120,34x170,34x220,34x280,48x150,48x210,48x270,48x330,64x170,64x230,64x290,64x350,80x200,80x260,80x320,80x380,96x220,96x280,96x340,96x400,120x250,120x320,120x390,120x460",
  "parallelWorkers": 10
}
```

## Charts

### score_by_network

![score_by_network](charts/score_by_network.svg)

### stable_ratio_by_network

![stable_ratio_by_network](charts/stable_ratio_by_network.svg)

### verdict_distribution

![verdict_distribution](charts/verdict_distribution.svg)

### worst_score_by_network

![worst_score_by_network](charts/worst_score_by_network.svg)

### unstable_or_oscillating_runs_by_network

![unstable_or_oscillating_runs_by_network](charts/unstable_or_oscillating_runs_by_network.svg)

### first_all_checks_round_by_network

![first_all_checks_round_by_network](charts/first_all_checks_round_by_network.svg)

### sustained_all_checks_round_by_network

![sustained_all_checks_round_by_network](charts/sustained_all_checks_round_by_network.svg)

### best_network_topology

![best_network_topology](charts/best_network_topology.svg)

### best_network_coverage

![best_network_coverage](charts/best_network_coverage.svg)

### best_network_stability

![best_network_stability](charts/best_network_stability.svg)

### best_network_connectivity_dynamics

![best_network_connectivity_dynamics](charts/best_network_connectivity_dynamics.svg)

### best_network_routing_dynamics

![best_network_routing_dynamics](charts/best_network_routing_dynamics.svg)

### best_network_theorem_status

![best_network_theorem_status](charts/best_network_theorem_status.svg)

### best_network_checks_status

![best_network_checks_status](charts/best_network_checks_status.svg)

### best_network_first_pass_round_by_check

![best_network_first_pass_round_by_check](charts/best_network_first_pass_round_by_check.svg)

### best_network_sustained_pass_round_by_check

![best_network_sustained_pass_round_by_check](charts/best_network_sustained_pass_round_by_check.svg)

### worst_network_topology

![worst_network_topology](charts/worst_network_topology.svg)

### worst_network_connectivity_dynamics

![worst_network_connectivity_dynamics](charts/worst_network_connectivity_dynamics.svg)

### worst_network_stability

![worst_network_stability](charts/worst_network_stability.svg)

### worst_network_routing_dynamics

![worst_network_routing_dynamics](charts/worst_network_routing_dynamics.svg)

### worst_network_theorem_status

![worst_network_theorem_status](charts/worst_network_theorem_status.svg)

### worst_network_checks_status

![worst_network_checks_status](charts/worst_network_checks_status.svg)

## Notes

- No additional score rationale for best run.

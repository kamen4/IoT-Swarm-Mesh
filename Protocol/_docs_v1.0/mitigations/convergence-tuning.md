# Convergence Tuning Guide

This guide collects practical tuning rules for charge-induced DOWN tree convergence when oscillations, late collapse, or unstable parent switching appear.

## Typical symptoms

- Parent flapping across a small set of neighbors.
- Structure converges, then collapses after many rounds.
- Bridge links between two clusters remain weak despite carrying most traffic.
- Duplicate count does not decrease over time.

## Why this happens

- Charges and neighbor estimates are monotonic between DECAY epochs.
- Without periodic DECAY, stale high values dominate parent selection.
- Hysteresis can be too small compared to absolute charge scale.
- Link quality that never adapts cannot represent traffic concentration.

## Recommended controls

- `decayIntervalSteps`: rounds between DECAY epochs (`0` disables DECAY).
- `decayPercent`: attenuation per epoch (`q <- q * (1 - decayPercent)`).
- `switchHysteresis`: absolute score margin needed to switch parent.
- `switchHysteresisRatio`: relative margin against estimate magnitude.
- `linkMemory`: persistence of edge usage history.
- `linkLearningRate`: adaptation speed of effective link quality.
- `linkBonusMax`: maximum parent-score bonus from stable high-traffic links.

## Stable baseline preset

Use this preset as a starting point for medium-size meshes:

- `decayIntervalSteps = 60`
- `decayPercent = 0.12`
- `switchHysteresis = 15`
- `switchHysteresisRatio = 0.03`
- `linkMemory = 0.94`
- `linkLearningRate = 0.20`
- `linkBonusMax = 45`

## Tuning strategy

1. If parent flapping stays high:
   - increase `switchHysteresis` first,
   - then increase `switchHysteresisRatio`.
2. If late collapse appears:
   - reduce `decayIntervalSteps` (more frequent DECAY),
   - or increase `decayPercent` slightly.
3. If bottleneck links are not reinforced:
   - increase `linkLearningRate`,
   - then increase `linkBonusMax` moderately.
4. If adaptation is too sticky after topology change:
   - reduce `linkMemory`.

## Metrics to watch

- `duplicates trend` should generally decrease.
- `flapping nodes` should trend toward zero.
- `gateway edge quality` should be higher than peripheral links.
- `strongest link` should often correspond to trunk or inter-cluster bridge edges.

## Parameter ranges (Python simulation parity)

The Python batch simulator uses two range layers:

- runtime normalization/clamping ranges (`config_normalizer.py`): permissive safety bounds for all inputs,
- optimization search ranges (`parameter_ranges.py`): narrower practical search space for faster batch tuning.

### Runtime normalization ranges

- `nodeCount`: 8..220
- `linkRadius`: 40..420
- `qForward`: 20..1800
- `deliveryProbability`: 0.05..1.0
- `penaltyLambda`: 0..250
- `switchHysteresis`: 0..260
- `switchHysteresisRatio`: 0.00..0.40
- `rootSourceCharge`: 250..3000
- `chargeDropPerHop`: 5..420
- `chargeSpreadFactor`: 0.02..1.0
- `seed`: 1..999999
- `roundsPerSecond`: 1..60
- `maxRounds`: 20..10000
- `decayIntervalSteps`: 0..2000
- `decayPercent`: 0.00..0.80
- `linkMemory`: 0.600..0.999
- `linkLearningRate`: 0.01..2.00
- `linkBonusMax`: 0..240

### Optimization search ranges

- `nodeCount`: 8..180
- `linkRadius`: 60..360
- `qForward`: 20..1300
- `deliveryProbability`: 0.15..1.0
- `penaltyLambda`: 0..150
- `switchHysteresis`: 0..120
- `switchHysteresisRatio`: 0.00..0.20
- `rootSourceCharge`: 500..2200
- `chargeDropPerHop`: 10..260
- `chargeSpreadFactor`: 0.05..1.0
- `decayIntervalSteps`: 0..500
- `decayPercent`: 0.00..0.40
- `linkMemory`: 0.750..0.995
- `linkLearningRate`: 0.05..0.80
- `linkBonusMax`: 0..120

If optimization repeatedly saturates near min/max values, expand the optimization ranges before drawing final conclusions.

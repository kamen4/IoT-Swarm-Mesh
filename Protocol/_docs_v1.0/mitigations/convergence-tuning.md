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

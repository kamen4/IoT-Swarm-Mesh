# Parameters Baseline And Governance

## Parameter families from source docs

- Forward eligibility and parent switching:
  - qForward
  - switchHysteresis
  - switchHysteresisRatio
- Charge decay controls:
  - decayIntervalSteps
  - decayPercent
- Additional tuning terms documented in mitigation context:
  - chargeDropPerHop
  - rootSourceCharge
  - penaltyLambda
  - deliveryProbability

## Governance rules

- Treat documented values as initial baseline, not immutable constants.
- Change parameters only through controlled rollout with observation windows.
- Keep parameter profile versioned and deployment-scoped.
- Use governance closure template:
  - Implementation/Spec/07-parameters/governance-rollout-spec.md

## OPEN DECISIONS

- Whether simulator-centric parameters are mandatory in firmware/server runtime.
- Rollback thresholds for unstable topologies in production.

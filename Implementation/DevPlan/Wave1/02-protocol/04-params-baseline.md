# Wave 1 Baseline Parameters (cand2_more_inertia)

## Fixed Defaults

| Parameter | Value |
| --- | --- |
| qForward | 443 |
| deliveryProbability | 0.21 |
| rootSourceCharge | 1808 |
| penaltyLambda | 72 |
| switchHysteresis | 42 |
| switchHysteresisRatio | 0.085 |
| chargeDropPerHop | 94 |
| chargeSpreadFactor | 0.08 |
| decayIntervalSteps | 91 |
| decayPercent | 0.22 |
| linkMemory | 0.89 |
| linkLearningRate | 0.5 |
| linkBonusMax | 50 |

## Parameter Tasks (0.5-1 day)

| Task ID | Objective | Inputs | Outputs | Done Criteria | Dependencies |
| --- | --- | --- | --- | --- | --- |
| W1-PAR-001 | Add baseline schema to config model | Implementation/Spec/02-protocol-core/07-charge-decay.md and Implementation/Spec/02-protocol-core/05-up-routing.md | Config schema with 13 fields | Schema matches names and types | none |
| W1-PAR-002 | Load baseline defaults on startup | Protocol/_theoreme_ai_search/try_3_baseline/candidate_sweep_requests/cand2_more_inertia.json | startup defaults applied | all 13 values match table | W1-PAR-001 |
| W1-PAR-003 | Add baseline consistency validation | config model | validation routine | invalid values rejected/logged | W1-PAR-001 |
| W1-PAR-004 | Expose baseline snapshot for diagnostics | runtime config | diagnostics output | snapshot shows all 13 values | W1-PAR-002 |
| W1-PAR-005 | Add tests for baseline lock | tests + defaults | test suite | tests fail on accidental drift | W1-PAR-002 |
| W1-PAR-006 | Document baseline override policy | authoring rules | policy note | override requires audit record | W1-PAR-005 |
| W1-PAR-007 | Add per-parameter bound checker | up/decay specs | bound checker | all 13 parameters validated against allowed ranges | W1-PAR-003 |
| W1-PAR-008 | Add missing-field fail-fast checks | baseline source artifact | fail-fast loader | startup fails safely on incomplete baseline input | W1-PAR-002 |
| W1-PAR-009 | Add parameter load audit events | audit log rules | audit event writer | each startup records full baseline load event | W1-PAR-002 |
| W1-PAR-010 | Add cold-start and warm-start load tests | lifecycle + params baseline | startup test suite | both startup modes apply consistent baseline defaults | W1-PAR-002 |
| W1-PAR-011 | Add parameter delta export routine | diagnostics + baseline snapshot | delta payload | deviations from baseline snapshot exported | W1-PAR-004 |
| W1-PAR-012 | Add runtime drift watchdog | validation matrix + diagnostics | drift watchdog | unexpected runtime parameter drift is detected and logged | W1-PAR-005 |
| W1-PAR-013 | Add safe baseline-revert routine | error policy + baseline artifact | revert routine | invalid runtime profile reverts to baseline safely | W1-PAR-012 |
| W1-PAR-014 | Add baseline parity fixture tests | baseline source JSON | fixture-based test suite | parity with source JSON verified in CI/local tests | W1-PAR-005 |

## Dependency Notes

- W1-PAR tasks are self-contained at planning level and depend only on existing Spec documents and baseline source artifact.
- Integration tasks consume W1-PAR-004 diagnostics output and W1-PAR-005 lock verification results.

## Source Pointers

- Protocol/_theoreme_ai_search/try_3_baseline/candidate_sweep_requests/cand2_more_inertia.json
- Protocol/_theoreme_ai_conclusion/report.md
- Implementation/Spec/02-protocol-core/05-up-routing.md
- Implementation/Spec/02-protocol-core/07-charge-decay.md

# Wave 1 Integration: Observability Minimum

## Scope

Define minimum observability needed for validation and release gates.

## Tasks (0.5-1 day each)

| Task ID | Objective | Inputs | Outputs | Done Criteria | Dependencies |
| --- | --- | --- | --- | --- | --- |
| W1-INT-201 | Define mandatory telemetry fields | validation matrix + specs | telemetry field catalog | catalog approved against validation rows | W1-FND-008 |
| W1-INT-202 | Implement audit coverage reporting | audit log + authoring rules | coverage report routine | percent of audited files is measurable | W1-INT-201 |
| W1-INT-203 | Add lifecycle and routing event correlation IDs | hub + routing specs | correlated event records | events traceable across components | W1-INT-201 |
| W1-INT-204 | Implement baseline snapshot diagnostics | params baseline file | baseline snapshot output | all 13 params emitted in diagnostics | W1-PAR-004 |
| W1-INT-205 | Validate observability against readiness gates | readiness checklist | observability gate report | readiness evidence includes observability artifacts | W1-INT-202 |
| W1-INT-206 | Define mandatory field dictionary per invariant | validation matrix + specs | field dictionary | each invariant mapped to required telemetry fields | W1-INT-201 |
| W1-INT-207 | Define event type catalog for onboarding/up/down/decay | protocol specs + diagnostics | event catalog | catalog covers all major protocol paths | W1-INT-201 |
| W1-INT-208 | Add per-node counters for rx/tx/drop/dedup/ttl | validation matrix + diagnostics | per-node counters | counters exported per node and cycle | W1-INT-206 |
| W1-INT-209 | Add convergence observability metrics | theorem + down-routing specs | convergence metrics | parent changes and convergence rounds exported | W1-INT-208 |
| W1-INT-210 | Add audit completeness integration checks | audit log + readiness checklist | audit completeness checker | missing audit coverage detected automatically | W1-INT-202 |
| W1-INT-211 | Add readiness evidence bundle writer | readiness checklist + validation matrix | evidence bundle generator | bundle contains links required for gate closure | W1-INT-205 |
| W1-INT-212 | Add required-field presence validator | field dictionary + exports | presence validator | export batches fail validation when fields are missing | W1-INT-206 |
| W1-INT-213 | Add correlation-id propagation tests | lifecycle/routing events | propagation test suite | same flow keeps correlation-id across components | W1-INT-203 |
| W1-INT-214 | Add observability overload guard tests | corner-cases + diagnostics | overload test suite | metric emission remains bounded under burst traffic | W1-INT-208 |

## Source Pointers

- Implementation/DevPlan/90-validation-matrix.md
- Implementation/DevPlan/92-audit-log.md
- Implementation/Spec/05-quality/01-validation-strategy.md

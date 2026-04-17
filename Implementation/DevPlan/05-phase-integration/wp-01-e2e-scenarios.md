# Integration WP-01 End-To-End Scenarios

## Goal

Validate full-path behavior across all components for critical protocol flows.

## Scenarios

S-01 Onboarding end-to-end
- Device onboarding artifact intake to Connected state completion.

S-02 Command lifecycle end-to-end
- User command to device action and response correlation.

S-03 Telemetry lifecycle end-to-end
- Device event emission to server persistence and user visibility.

S-04 Sleepy-device lifecycle end-to-end
- Wake, pull, pending command retrieval, and completion.

## Tasks

T1. Scenario specification
- T1.1 Define preconditions and trigger inputs.
- T1.2 Define expected outputs and acceptance assertions.

T2. Scenario execution
- T2.1 Execute all scenarios under nominal conditions.
- T2.2 Execute all scenarios under degraded conditions.

T3. Defect handling
- T3.1 Classify defects by severity and ownership.
- T3.2 Feed defects back to workstream backlog.

## Deliverables

- End-to-end scenario test report.
- Defect triage report.

## Acceptance criteria

- All critical scenarios pass at agreed acceptance level.

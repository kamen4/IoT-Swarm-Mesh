# Full Product Master Plan

This plan extends Wave1 to full product delivery.

## Tracks

- FP-ARCH: architecture freeze and interface contracts
- FP-ESP: device library and firmware integration
- FP-GW: gateway bridge runtime
- FP-HUB: dockerized server stack and business services
- FP-DATA: SQL and TSDB persistence pipeline
- FP-BOT: telegram role-driven control plane
- FP-OBS: observability and dashboard delivery
- FP-QA: end-to-end validation and resilience checks
- FP-REL: release packaging and deployment handoff

## Execution table

| Task ID | Objective | Outputs | Dependencies |
| --- | --- | --- | --- |
| FP-ARCH-001 | Freeze full-product architecture contract | architecture contract note | none |
| FP-ARCH-002 | Freeze UART bridge message boundary | UART boundary contract | FP-ARCH-001 |
| FP-ARCH-003 | Freeze Redis topic contract | topic contract matrix | FP-ARCH-001 |
| FP-ARCH-004 | Freeze service responsibility map | service ownership map | FP-ARCH-001 |
| FP-ARCH-005 | Freeze baseline and tuning policy for production | baseline policy document | FP-ARCH-001 |
| FP-ESP-001 | Implement device envelope codec package | codec package | FP-ARCH-001 |
| FP-ESP-002 | Implement device onboarding handlers | onboarding handler module | FP-ESP-001 |
| FP-ESP-003 | Implement device IO message handlers | IO handler module | FP-ESP-001 |
| FP-ESP-004 | Implement device UP forwarding module | UP module | FP-ESP-001 |
| FP-ESP-005 | Implement device DOWN forwarding module | DOWN module | FP-ESP-001 |
| FP-ESP-006 | Implement device dedup and ttl guards | guard module | FP-ESP-004 |
| FP-ESP-007 | Implement device diagnostics export | diagnostics module | FP-ESP-003 |
| FP-ESP-008 | Add device integration template project | template package | FP-ESP-002 |
| FP-GW-001 | Implement UART ingress pipeline | ingress component | FP-ARCH-002 |
| FP-GW-002 | Implement UART egress pipeline | egress component | FP-ARCH-002 |
| FP-GW-003 | Implement mesh metadata preservation | metadata map layer | FP-GW-001 |
| FP-GW-004 | Implement control-message forwarding hooks | control hooks | FP-GW-002 |
| FP-GW-005 | Implement queue limits and overload guards | guard configuration | FP-GW-001 |
| FP-GW-006 | Implement gateway diagnostics and health metrics | gateway metrics export | FP-GW-005 |
| FP-HUB-001 | Create docker-compose baseline for HUB services | compose baseline | FP-ARCH-004 |
| FP-HUB-002 | Implement UART bridge service container | UART service container | FP-HUB-001 |
| FP-HUB-003 | Integrate Redis pub/sub service | Redis service integration | FP-HUB-001 |
| FP-HUB-004 | Implement ASP.NET business server skeleton | business server skeleton | FP-HUB-001 |
| FP-HUB-005 | Implement onboarding workflow service | onboarding service | FP-HUB-004 |
| FP-HUB-006 | Implement command routing service | command service | FP-HUB-004 |
| FP-HUB-007 | Implement telemetry ingestion service | telemetry service | FP-HUB-004 |
| FP-HUB-008 | Implement lifecycle state service | lifecycle service | FP-HUB-005 |
| FP-HUB-009 | Implement RBAC middleware and guards | rbac middleware | FP-HUB-004 |
| FP-HUB-010 | Implement service-level audit events | audit event stream | FP-HUB-008 |
| FP-DATA-001 | Define SQL schema for users/devices/lifecycle | SQL schema spec | FP-HUB-004 |
| FP-DATA-002 | Implement SQL migrations and bootstrap | migration scripts | FP-DATA-001 |
| FP-DATA-003 | Define protocol metadata persistence model | metadata model | FP-DATA-001 |
| FP-DATA-004 | Integrate business server with SQL layer | SQL integration | FP-DATA-002 |
| FP-DATA-005 | Define TSDB measurement schema | TSDB schema document | FP-HUB-007 |
| FP-DATA-006 | Integrate telemetry writes to TSDB | TSDB writer integration | FP-DATA-005 |
| FP-DATA-007 | Add data retention and backup policy docs | retention policy | FP-DATA-002 |
| FP-BOT-001 | Implement Telegram command routing layer | command router | FP-HUB-004 |
| FP-BOT-002 | Implement user command set | user command handlers | FP-BOT-001 |
| FP-BOT-003 | Implement dedicated-admin command set | dedicated-admin handlers | FP-BOT-001 |
| FP-BOT-004 | Implement admin role command set | admin handlers | FP-BOT-001 |
| FP-BOT-005 | Implement role assignment flow | role assignment service | FP-BOT-004 |
| FP-BOT-006 | Implement bot-side audit trail | bot audit events | FP-BOT-003 |
| FP-OBS-001 | Define full-product observability field set | observability dictionary | FP-ARCH-004 |
| FP-OBS-002 | Implement lifecycle dashboards | lifecycle dashboard set | FP-DATA-006 |
| FP-OBS-003 | Implement routing stability dashboards | routing dashboard set | FP-DATA-006 |
| FP-OBS-004 | Implement onboarding and failure dashboards | onboarding dashboard set | FP-DATA-006 |
| FP-OBS-005 | Implement gateway health dashboards | gateway dashboard set | FP-GW-006 |
| FP-QA-001 | Build full-product integration harness profile | harness profile | FP-ESP-008 |
| FP-QA-002 | Run onboarding e2e tests through gateway and hub | onboarding test report | FP-HUB-005 |
| FP-QA-003 | Run command/response e2e tests | command e2e report | FP-HUB-006 |
| FP-QA-004 | Run telemetry e2e tests | telemetry e2e report | FP-HUB-007 |
| FP-QA-005 | Run role and permission e2e tests | RBAC e2e report | FP-BOT-005 |
| FP-QA-006 | Run resilience tests (queue pressure, malformed frames) | resilience report | FP-GW-005 |
| FP-QA-007 | Attach evidence to validation matrix | validation evidence links | FP-QA-002 |
| FP-QA-008 | Close provisional decisions and blockers | blocker closure record | FP-QA-007 |
| FP-REL-001 | Assemble deployable compose package | release package | FP-HUB-001 |
| FP-REL-002 | Produce environment configuration templates | env template set | FP-REL-001 |
| FP-REL-003 | Produce operator runbook | operator runbook | FP-OBS-005 |
| FP-REL-004 | Produce incident and rollback playbook | rollback playbook | FP-QA-006 |
| FP-REL-005 | Final readiness gate review | readiness review report | FP-QA-008 |
| FP-REL-006 | Final full-product handoff | handoff package | FP-REL-005 |

## Source Pointers

- Implementation/Spec/06-full-product/01-full-product-spec.md
- Protocol/_docs_v1.0/reference/architecture.md
- Protocol/_docs_v1.0/reference/users.md
- Protocol/_docs_v1.0/algorithms/01-onboarding.md

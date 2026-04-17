# Test Strategy Baseline

## Objective

Define mandatory verification coverage that each implementation phase must satisfy before gate pass.

## Coverage layers

1. Requirements coverage
- Every work package task maps to at least one verification activity.

2. Flow coverage
- Onboarding flow verification.
- Command and acknowledgement flow verification.
- Telemetry flow verification.
- Sleepy-device pull flow verification.

3. Failure-path coverage
- Timeout handling verification.
- Retry and backoff behavior verification.
- Recovery procedure verification.

4. Security and governance coverage
- Role enforcement verification.
- Audit event verification.
- Key-material handling verification boundaries.

## Phase evidence requirements

- Phase 1:
  - Test scope matrix approved.
- Phase 2-4:
  - Workstream-level verification evidence recorded.
- Phase 5:
  - End-to-end scenario verification report.
- Phase 6:
  - Production readiness verification report.

## Exit criteria

- No phase gate can pass without listed evidence for its phase.

## Test scope matrix by phase and workstream

Phase 1
- Foundation workstream:
  - Required evidence: baseline test scope matrix and critical flow map.

Phase 2
- Server workstream:
  - Required evidence: onboarding, command, telemetry, and role-policy verification report.

Phase 3
- Gateway workstream:
  - Required evidence: bridge, routing, queue-pressure, and recovery verification report.

Phase 4
- Device library workstream:
  - Required evidence: onboarding, key handling, IO contract, and sleepy-device verification report.

Phase 5
- Integration workstream:
  - Required evidence: end-to-end multi-component scenario report and parameter rollout verification report.

Phase 6
- Launch workstream:
  - Required evidence: production readiness verification report and cutover rehearsal report.

## Critical flow to obligation mapping

Flow F-01 Onboarding
- Obligation O-01: verify deterministic stage transitions Pending -> Verified -> Connected.
- Obligation O-02: verify failure handling for discovery timeout and verification failure.

Flow F-02 Command path
- Obligation O-03: verify IO_GET and IO_SET request/response correlation.
- Obligation O-04: verify retry and timeout behavior.

Flow F-03 Telemetry path
- Obligation O-05: verify IO_EVENT ingestion, normalization, and persistence handoff.
- Obligation O-06: verify burst-handling and backpressure behavior.

Flow F-04 Sleepy-device pull path
- Obligation O-07: verify pending-queue retrieval through PULL/PULL_R behavior.
- Obligation O-08: verify pending command expiration and ordering guarantees.

Flow F-05 Governance and security controls
- Obligation O-09: verify role enforcement matrix.
- Obligation O-10: verify audit event generation for onboarding, role changes, and revoke actions.
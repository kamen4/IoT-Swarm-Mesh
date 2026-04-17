# WP-04 Open-Decision Closure

## Goal

Close implementation-blocking open decisions from specification package.

## Blocking decision list

- D-01 UART frame contract details.
- D-02 SPAKE2 implementation profile details.
- D-03 Timing policy for beacon/decay/parent-expiry windows.
- D-04 Role command permission matrix.
- D-05 Parameter governance ownership and rollout policy.
- D-06 Persistence baseline for lifecycle, command, and telemetry records.
- D-07 Test strategy baseline per phase and per workstream.

## Tasks

T1. Decision framing
- T1.1 Capture each open decision with impact and affected components.
- T1.2 Assign owner and target closure date.

T2. Option analysis
- T2.1 Define acceptable options per decision constrained by source docs.
- T2.2 Assess compatibility impact across server/gateway/library.

T3. Decision finalization
- T3.1 Select option and document rationale.
- T3.2 Update affected spec files.
- T3.3 Mark decision as closed in register.

T4. Test strategy baseline finalization
- T4.1 Produce test scope matrix by phase and workstream.
- T4.2 Map critical protocol flows to verification obligations.
- T4.3 Publish baseline in governance package.

## Deliverables

- Closed decision register.
- Updated spec package with resolved blocking decisions.
- Decision-pack outputs:
	- Implementation/Spec/05-gateway/uart-frame-decision-pack.md
	- Implementation/Spec/03-protocol/spake2-interoperability-decision-pack.md
	- Implementation/Spec/04-server/role-permission-matrix-template.md
	- Implementation/Spec/07-parameters/governance-rollout-spec.md

## Acceptance criteria

- No unresolved blocking decision remains for phase 2, phase 3, or phase 4.

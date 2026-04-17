# Development Plan Index

This folder defines the end-to-end implementation plan for production system development.

Planning principles:
- Plan references only documented architecture and protocol behavior.
- Each phase includes work packages, deliverables, and phase gate criteria.
- Open decisions are explicitly tracked and resolved through governance.

## Folder map

- 00-governance: program rules, dependencies, risk management
- 01-phase-foundation: source baseline and implementation decision closure
- 02-phase-server: server workstream implementation plan
- 03-phase-gateway: gateway workstream implementation plan
- 04-phase-device-library: device library workstream implementation plan
- 05-phase-integration: cross-component integration and validation
- 06-phase-launch: production readiness and cutover
- 07-backlog: unresolved and deferred scope management

## Global execution order

1. Governance setup
2. Foundation closure
3. Parallel execution:
   - Server stream
   - Gateway stream
   - Device library stream
4. Integration phase
5. Launch phase

## Program-level deliverables

- Complete implementation backlog with acceptance criteria.
- Closed protocol open-decision list required for implementation.
- Phase-gated progress records for all streams.

# Server Specification

## Functional scope

- User command intake through Telegram integration.
- Device onboarding orchestration and state transitions.
- Command dispatch to gateway and response correlation.
- Telemetry ingestion and persistence.

## Required capabilities

- Message builder/parser aligned with protocol envelope.
- End-to-end tag validation for authenticated flows.
- Device model registry built from PROTO_R.
- Sleepy device pending command management.

## Authorization model

- Implement role logic for User, DedicatedAdmin, Admin.
- Enforce role restrictions at command entry points.
- Fill and approve role matrix template:
	- Implementation/Spec/04-server/role-permission-matrix-template.md

## Operational control

- Configuration surface for documented protocol parameters.
- Administrative controls for device revoke/re-onboard.

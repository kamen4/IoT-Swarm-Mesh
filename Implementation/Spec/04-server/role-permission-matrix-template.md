# Role Permission Matrix Template

## Purpose

Provide explicit authorization mapping for User, DedicatedAdmin, and Admin roles.

## Matrix template

| Operation | User | DedicatedAdmin | Admin | Notes |
|-----------|------|----------------|-------|-------|
| Onboarding request handling | TBD | TBD | TBD | |
| Device revoke action | TBD | TBD | TBD | |
| Device command execution | TBD | TBD | TBD | |
| Device status query | TBD | TBD | TBD | |
| Parameter update action | TBD | TBD | TBD | |
| Role assignment change | TBD | TBD | TBD | |

## Enforcement points

- Command intake path.
- Administrative action path.
- Parameter governance path.

## Acceptance criteria

- Matrix has no empty operation rows.
- Matrix is approved and referenced by:
  - Implementation/Spec/04-server/spec.md
  - Implementation/DevPlan/02-phase-server/wp-04-access-control.md

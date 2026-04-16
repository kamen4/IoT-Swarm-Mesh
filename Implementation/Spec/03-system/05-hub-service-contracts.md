# HUB Service Contracts (Full Product)

## Objective

Define full-product service boundaries and runtime contracts for dockerized HUB stack.

## Service Set

- UART bridge service
- Redis pub/sub service
- Business server service
- SQL database service
- TSDB service
- Dashboard service
- Telegram interface service

## Contract Rules

- Requirement: Services MUST start in dependency order: data stores -> message bus -> bridge -> business server -> presentation and dashboards.
  - Source: Protocol/_docs_v1.0/reference/architecture.md
- Requirement: Business server MUST be the lifecycle authority for device state transitions.
  - Source: Protocol/_docs_v1.0/reference/architecture.md
- Requirement: UART bridge MUST be the only mesh ingress/egress path for HUB services.
  - Source: Protocol/_docs_v1.0/reference/architecture.md
- Requirement: Business server MUST be the only direct writer to persistent business data stores.
  - Source: Protocol/_docs_v1.0/reference/architecture.md
- Requirement: Service health endpoints MUST expose readiness and dependency health state.
  - Source: Protocol/_docs_v1.0/mitigations/corner-cases.md

## Configuration Rules

- Requirement: All inter-service addresses MUST be environment-driven and auditable.
  - Source: Protocol/_docs_v1.0/reference/architecture.md
- Requirement: Runtime role checks for user actions MUST be enforced at business server boundary.
  - Source: Protocol/_docs_v1.0/reference/users.md

## Source Pointers

- Protocol/_docs_v1.0/reference/architecture.md
- Protocol/_docs_v1.0/reference/users.md
- Implementation/Spec/06-full-product/01-full-product-spec.md

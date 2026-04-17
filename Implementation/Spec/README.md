# Implementation Spec Index

This folder contains implementation-oriented specifications derived from protocol documentation.

Primary source set:
- Protocol/_docs_v1.0
- Protocol/_theoreme_ai_conclusion (context only)
- Protocol/_theoreme_ai_search (context only)

Rule of interpretation:
- If a requirement is explicit in source docs, it is listed as REQUIRED.
- If source docs leave a gap, it is listed as OPEN DECISION.
- No additional technology stack is imposed in this specification.

## Folder map

- 00-sources: source map and scope boundaries
- 01-system: goals, context, actors
- 02-architecture: runtime architecture and component contracts
- 03-protocol: protocol-level implementation contracts
- 04-server: server-side implementation requirements
- 05-gateway: gateway implementation requirements
- 06-device-library: device library requirements
- 07-parameters: parameter baseline and governance
- 08-quality-risks: constraints and unresolved items
- 09-traceability: requirement-to-source matrix

Decision-pack files for blocking open items:
- 03-protocol/spake2-interoperability-decision-pack.md
- 04-server/role-permission-matrix-template.md
- 05-gateway/uart-frame-decision-pack.md
- 07-parameters/governance-rollout-spec.md

## Reading order

1. 00-sources/source-map.md
2. 01-system/goals-and-context.md
3. 02-architecture/system-architecture.md
4. 03-protocol/*.md
5. 04-server, 05-gateway, 06-device-library
6. 07-parameters and 08-quality-risks
7. 09-traceability/requirements-matrix.md

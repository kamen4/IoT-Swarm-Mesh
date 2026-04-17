# Dependency Map

## Primary dependency graph

1. Foundation phase depends on:
- Source documentation baseline confirmation.
- Open-decision inventory.

2. Server phase depends on:
- Foundation closure of protocol and architecture interpretation.

3. Gateway phase depends on:
- Foundation closure of UART bridge contract and routing contract interpretation.

4. Device library phase depends on:
- Foundation closure of onboarding and key handling interpretation.

5. Integration phase depends on:
- Completion of server, gateway, and device library phase gates.

6. Launch phase depends on:
- Integration gate completion.

## Cross-stream dependencies

- Server command model depends on device interaction model availability.
- Gateway frame bridge depends on server framing contract decisions.
- Sleepy-device behavior requires aligned server queue policy and library poll policy.

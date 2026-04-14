# IoT Swarm Mesh Protocol Documentation

Documentation for a secure, on-premises ESP-NOW mesh protocol with swarm intelligence routing and Telegram-based remote management.

## Quick Navigation

### Core Concepts

- **[Architecture](reference/architecture.md)** — Network topology, HUB design, data flow
- **[Glossary](00-glossary.md)** — Key terms and definitions
- **[Overview](reference/overview.md)** — Problem statement and solution summary

### Algorithms

1. **[Onboarding](algorithms/01-onboarding.md)** — Device registration and SPAKE2 authentication
2. **[Message Envelope](algorithms/02-message-envelope.md)** — Frame structure, HMAC, replay protection
3. **[UP Routing](algorithms/03-up-routing.md)** — Swarm-based charge routing toward gateway
4. **[DOWN Routing](algorithms/04-down-routing.md)** — Tree-first broadcast from gateway
5. **[Identity & Security](algorithms/05-identity-security.md)** — Server-verified identity; `S_PASSWORD`-based authentication

### Implementation Guidance

- **[Protocol (Wire Format)](reference/protocol.md)** — `msgType` registry, payload encoding, TAG semantics, fragmentation
- **[Byte Sizes & Encoding](reference/byte-sizes.md)** — Binary format specifications
- **[Users & Roles](reference/users.md)** — Access control model
- **[Corner Cases & Mitigations](mitigations/corner-cases.md)** — Failure modes and practical solutions
- **[Convergence Tuning](mitigations/convergence-tuning.md)** — Practical settings for oscillation and collapse prevention

## System Overview

The protocol enables a **closed, on-premises IoT mesh** where:

- **ESP devices** communicate over ESP-NOW (no access point required)
- **One gateway device** bridges to a **HUB (host)** via UART
- **Users** control the system via Telegram, with role-based access (User / Dedicated Admin / Admin)
- **No cloud dependency**: core functionality works entirely locally with optional Telegram remote access

See [Architecture](reference/architecture.md) for detailed system diagram and HUB container layout.

## Key Design Principles

- **Resilient mesh**: charge-based best-neighbor routing for UP traffic; tree-first for DOWN
- **Scalability**: devices keep only neighbor state, no global routing tables
- **Security**: end-to-end HMAC per device (`S_PASSWORD`), SPAKE2 onboarding; network membership is verified by the server
- **Efficiency**: charge-based neighbor selection, lightweight forwarding rules, deduplication by `(originMac, msgId)`

## File Organization

| Category       | Purpose                                                      |
| -------------- | ------------------------------------------------------------ |
| `algorithms/`  | Core protocol procedures (onboarding, routing, identity)     |
| `reference/`   | Technical specifications (byte layouts, system architecture) |
| `mitigations/` | Failure modes and practical solutions                        |

For implementation details, refer to the specific algorithm file. For troubleshooting, see [Corner Cases & Mitigations](mitigations/corner-cases.md).

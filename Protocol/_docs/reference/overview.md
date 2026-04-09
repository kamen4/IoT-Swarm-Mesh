# Overview

## Problem

The project targets an IoT deployment where many ESP devices must communicate over **ESP-NOW** as a self-organizing mesh, while being **remotely managed by a small set of authorized users** via Telegram.

Key constraints and challenges:

- The solution should remain **fully on-premises**: core functionality must work without any cloud dependency and can operate **without Internet access** (local host + local radio network).
- ESP-NOW provides link-layer delivery but does not provide end-to-end device identity, authorization, or a mesh routing scheme.
- Devices can be multi-hop away from the gateway, and topology can change; routes should emerge without maintaining global routing tables.
- Intermediate forwarders must be able to modify routing metadata (e.g., `ttl`, last hop), without breaking end-to-end authenticity.
- Device onboarding must be secure and user-friendly (QR + one-time connection string), with a way to bind a physical device to a server-side record.
- The system is **closed**: access control, roles, and audit/logging matter as much as packet delivery.

Why ESP-NOW (vs “regular” Wi‑Fi networking) is attractive for this use case:

- No access point (AP) is required for basic device-to-device delivery: the mesh can exist even where there is no Wi‑Fi infrastructure.
- Lower connection/management overhead than IP networking in small payload/command scenarios (no DHCP/TCP stack requirement), which is useful for constrained devices.
- Direct addressing by MAC and a small frame model fits a hop-by-hop forwarding design.

Trade-offs to accept up front:

- Smaller payload budget per frame and more responsibility on the protocol (fragmentation/ordering/application semantics if needed).
- Channel/interference constraints are shared with 2.4 GHz Wi‑Fi; reliability is topology- and environment-dependent.

## Solution

The solution is a **two-layer protocol** plus a hub backend:

1. **Architecture**
   - **Mesh**: ESP devices exchange frames over ESP-NOW.
   - **Gateway device**: one ESP node physically connected to the host via UART; it bridges mesh traffic to the server.
   - **HUB (host)**: PC / Raspberry Pi / laptop running the server stack; user interacts through a Telegram bot.

   The entire system is designed to run **on-premises**: the HUB (host), databases, and bot backend live locally. Internet access is not required for the mesh itself; remote control can be provided via Telegram when connectivity exists, but the networking and security model do not depend on a cloud service.

2. **Secure onboarding (device registration)**
   - A device exposes a configuration page (from QR) that yields a `CONNECTION_STRING` containing the device MAC and a hash of a random `CONNECTION_KEY`.
   - The user sends the `CONNECTION_STRING` to the bot; the server broadcasts `FIND` to locate the device.
   - The server and device perform **SPAKE2** to derive a per-device secret `S_PASSWORD`.
   - After verification, the server fetches the device's **Interaction Protocol** (`PROTO/PROTO_R`) and moves the device to `Connected`, then sends `START`.

3. **End-to-end message authenticity (server ↔ device)**
   - After SPAKE2, device commands and telemetry are authenticated with `HMAC(S_PASSWORD, SECURE_HEADER | PAYLOAD)`.
   - Replay protection uses a per-session `seq` validated by endpoints.

4. **Mesh forwarding (swarm routing)**
   - Each ESP-NOW frame uses a split header: a **mutable** `ROUTING_HEADER` (not covered by end-to-end HMAC) and an **immutable** `SECURE_HEADER` + payload (covered by HMAC).
   - **UP delivery** is **charge-based swarm forwarding**: nodes propagate UP packets towards the gateway via the top neighbors by `q_up`, using `ttl` + deduplication to prevent storms.
   - **DOWN delivery** (gateway → device unicast commands) is **tree-first**: a gradient/tree rooted at the gateway is formed using `BEACON`, and nodes forward DOWN only to their children (loop-free by construction).
   - Gateway-originated mesh-control broadcasts (e.g., `BEACON`, `DECAY`, onboarding `FIND`) may still use controlled multi-path dissemination so they can converge without relying on an already-formed tree.
   - A network-wide `DECAY` epoch prevents unbounded charge growth and helps convergence.

Why a swarm-style, charge-based approach (vs common alternatives) is a good fit here:

- **No global routes**: nodes keep only neighbor state, which scales better than maintaining full routing tables on constrained devices.
- **Topology tolerance**: multi-path propagation to the “top neighbors” is more resilient than a strict tree where a single parent failure can isolate a subtree.
- **Less waste than flooding**: selecting only a fraction of neighbors reduces the broadcast-storm behavior while still keeping redundancy.
- **Self-stabilization**: `DECAY` prevents unbounded metric growth and helps the network re-balance when conditions change.

As with any mesh routing, this is a trade-off: compared to a single-path route, it can use more airtime due to controlled replication. The protocol mitigates this with `ttl` and deduplication (`(originMac, msgId)` cache).

Note: the document specifies authenticity/integrity via HMAC; payload encryption/confidentiality is not defined here.

# Identity & Security (Server-Verified)

This protocol intentionally uses a **server-verified identity model**:

- There is **no network-wide CA**, **no device certificates**, and **no shared network/group keys**.
- After onboarding, the only cryptographic secret used by the protocol is the per-device `S_PASSWORD` shared between that device and the server.

The mesh is treated as an **untrusted transport** layer: intermediate forwarders do not validate end-to-end authenticity.

---

## 1) Keys and trust boundaries

- `CONNECTION_KEY`
  - Onboarding secret embedded in the device QR code.
  - Used only as the SPAKE2 password input; it is never transmitted.

- `S_PASSWORD`
  - Per-device secret established by SPAKE2 between the device and the server.
  - Used for end-to-end message authentication (`TAG`).
  - NOT shared with other devices; neighbors cannot verify it locally.

---

## 2) End-to-end authentication (server ↔ device)

After SPAKE2, end-to-end authenticated messages use:

$$
TAG = \mathrm{Trunc16}(\mathrm{HMAC\_SHA256}(S\_PASSWORD,\; SECURE\_HEADER\;|\;PAYLOAD)).
$$

Requirements:

- Intermediate forwarders MUST NOT attempt to validate or generate `TAG`.
- Any statement like “device X is authenticated” is only meaningful **at the endpoints** (server and the device).

---

## 3) Mesh-control messages are unauthenticated hints

Mesh-control broadcasts such as `BEACON`, `DECAY`, `WAKE`, `HELLO` are typically sent with `TAG = 0`.

Receivers MUST treat them as **untrusted hints**:

- apply strict TTL and hop-by-hop deduplication;
- per-type rate limiting;
- conservative topology updates (hysteresis, ignore one-off samples).

They MUST NOT be treated as proof of identity / membership.

---

## 4) Server-assisted peer membership check (optional)

Problem: device **A** wants to know whether a peer **B** “belongs to the network”.

Because A does not know `S_PASSWORD_B`, A cannot validate any B-produced HMAC locally. The check must be **server-assisted**.

One possible minimal flow (message names are illustrative; implement as application payloads or dedicated `msgType`s if needed):

1. A generates a random nonce `Na` and sends it to B as a best-effort mesh-control hint (`TAG = 0`).
2. B replies with `(Na, proof)` where:

$$
proof = \mathrm{Trunc16}(\mathrm{HMAC\_SHA256}(S\_PASSWORD_B,\; "PEER\_PROOF"\;|\;mac_A\;|\;Na)).
$$

3. A sends `(mac_B, Na, proof)` to the server using an end-to-end authenticated request (A uses its own `S_PASSWORD_A`).
4. The server recomputes `proof` using stored `S_PASSWORD_B` and returns a boolean verdict to A.

Notes:

- The nonce prevents trivial replay.
- If the server is unreachable, treat B as untrusted for security-critical decisions.

---

## 5) Key compromise

- If `S_PASSWORD` of a device is extracted, an attacker can impersonate that device to the server and forge end-to-end authenticated traffic.
- Mitigate with server-side revocation (deny-list by `originMac`) and requiring re-onboarding (new `CONNECTION_KEY` → new SPAKE2 → new `S_PASSWORD`).

Because there are no network-wide shared secrets, compromise impact is per-device.

---

## 6) Cryptographic profile (minimal)

- SPAKE2 (onboarding only)
- SHA-256
- HMAC-SHA256 truncated to 16 bytes (`Trunc16`)

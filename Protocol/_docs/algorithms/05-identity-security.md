# Offline Device Identity (Device-to-Device Auth)

## Device identity without server involvement

Goal: allow a device to verify that another device is a **registered member of the network** (e.g. after deep-sleep wake + broadcast discovery), without querying the server and without storing per-pair keys.

Use a network-wide CA and per-device keys:

- `NET_CA_PUB` - network CA public key (same on all devices). Provisioned at manufacturing time or delivered after SPAKE2 (protected by `S_PASSWORD`).
- `DEV_PRIV`, `DEV_PUB` - per-device signing key pair (unique per device).
- `DEV_CERT` - compact certificate signed by the CA that binds `(originMac -> DEV_PUB)` plus optional metadata (expiry, capabilities).

With this scheme:

- Any device can verify another device offline by checking `DEV_CERT` with `NET_CA_PUB`, then verifying a signature made with `DEV_PRIV`.
- Compromise of one device does **not** allow impersonation of other devices (attacker only gains that device's `DEV_PRIV`).

Broadcast discovery / wake:

- `WAKE` (broadcast to `FF:FF:FF:FF:FF:FF`) includes `DEV_CERT` (or its hash/id) and a signature `SIG_DEV`.
- To prevent replay, neighbors MAY require a short challenge-response:
  - neighbor sends `CHALLENGE(nonce)` (unicast)
  - waking device replies `PROVE(sig(nonce))`

## Optional: shared group key for cheap filtering (trade-off)

If you still want a fast symmetric check for mesh-control traffic, you MAY add:

- `NET_GROUP_KEY` - shared network key provisioned after SPAKE2.

Important limitation: if `NET_GROUP_KEY` is extracted from one device, an attacker can forge group-authenticated control messages. Therefore `NET_GROUP_KEY` MUST NOT be the only mechanism used to validate device identity.

## Cryptographic profile

This section defines a concrete crypto profile for the **offline device identity** scheme (`NET_CA_PUB` + `DEV_CERT` + device signatures) and the optional `NET_GROUP_KEY`.

### Primitives

- Hash: `SHA-256`.
- KDF: `HKDF-SHA256`.
- End-to-end MAC: `HMAC-SHA256`, truncated to 16 bytes.
- Device / CA signatures: `Ed25519` (32-byte public key, 64-byte signature).

### Key derivation from SPAKE2

Treat SPAKE2 output as a high-entropy shared secret and derive keys via HKDF:

- `S_SHARED = SPAKE2(...)`
- `S_PASSWORD = HKDF(S_SHARED, info="mesh-e2e-hmac", len=32)`

Optionally: if you need to provision secrets like `NET_GROUP_KEY` over the air without leaking them, derive an encryption key too:

- `S_ENC_KEY = HKDF(S_SHARED, info="mesh-e2e-enc", len=32)`

If you do not implement end-to-end encryption, then any secret delivered over the air (including `NET_GROUP_KEY`) is observable and should instead be provisioned at manufacturing time.

### `DEV_CERT` (compact certificate)

Minimum fields:

- `originMac` (6 bytes)
- `DEV_PUB` (32 bytes)
- `notAfter` (e.g., unix minutes, 4 bytes)
- optional `caps/flags` (1-2 bytes)
- `CA_SIG` (64 bytes) — CA signature over the preceding fields

Identifier:

- `certId = Trunc64(SHA256(DEV_CERT))` (8 bytes) or `Trunc128` (16 bytes)

Neighbors SHOULD cache `DEV_CERT` by `certId` so broadcasts can carry `certId` instead of the full cert once learned.

### Signing mesh-control messages

For identity-grade control (use for `WAKE`, and optionally for `BEACON/DECAY`):

- Sender builds `PAYLOAD` including the fields relevant for replay control (see below) and excluding the signature.
- Computes `SIG_DEV = Ed25519.Sign(DEV_PRIV, SHA256(SECURE_HEADER | PAYLOAD_NO_SIG))`.
- Appends `SIG_DEV` to `PAYLOAD`.

Verification:

1. If full cert is present: verify `DEV_CERT` using `NET_CA_PUB`, check expiry, and check `originMac` matches `SECURE_HEADER.originMac`.
2. Else if `certId` is present: lookup cached `DEV_CERT` by `certId`.
3. Verify `SIG_DEV` using the `DEV_PUB` from the certificate.

### Anti-replay guidance (broadcast-friendly)

Broadcasts cannot rely on a synchronized `seq`, so use one of:

- **Challenge-response (strongest)**: neighbor unicasts `CHALLENGE(nonce)` and accepts `PROVE(sig(nonce))`.
- **Stateless-ish token**: include `wakeNonce` (random 8-16 bytes) and `bootCounter` (monotonic) in `WAKE`, and keep a small LRU cache of recently-seen `(originMac, wakeNonce)` for a short window.

For gateway-originated control (`BEACON/DECAY`), include a monotonic `epoch`/`counter` and accept only within a sliding window to prevent replay storms.

### Size considerations

`Ed25519` signatures are 64 bytes; a minimal `DEV_CERT` can be large enough to exceed a single ESP-NOW frame once combined with a signature and encoding overhead. If a single ESP-NOW frame cannot fit `DEV_CERT` + signature in one go:

- Send full `DEV_CERT` only when first discovered (or fragmented), then use `certId` in subsequent broadcasts.
- Prefer unicast challenge-response after discovery to avoid repeating large certs on broadcast.

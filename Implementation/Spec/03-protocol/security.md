# Security Contract

## Model from source docs

- Per-device secret model.
- No network-wide CA or certificate chain in v1 model.
- End-to-end authenticity and integrity via HMAC-SHA256 truncated tag.

## Authentication scope

- End-to-end messages after onboarding:
  - TAG = Trunc16(HMAC-SHA256(S_PASSWORD, SECURE_HEADER | PAYLOAD)).
- Mesh-control messages:
  - TAG=0 path treated as untrusted hints.

## Forwarder behavior

- Intermediate nodes forward without end-to-end tag verification.

## OPEN DECISIONS

- Confidentiality policy (payload encryption) is not defined in v1 docs.
- Key storage hardening profile per device class.

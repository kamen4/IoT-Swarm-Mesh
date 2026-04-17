# Device Library Sleepy Device Model

## Required behavior

- Support wake notification path.
- Use pull-based command retrieval when push delivery is not available.
- Preserve command ordering guarantees as defined by server policy.

## Power-state constraints

- Minimize active window while preserving reliable command retrieval.
- Preserve enough protocol state for safe wake-resume behavior.

## OPEN DECISIONS

- Poll cadence profile per device class.
- Pending command expiration behavior alignment with server policy.

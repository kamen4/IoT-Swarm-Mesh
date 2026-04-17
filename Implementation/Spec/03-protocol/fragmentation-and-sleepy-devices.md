# Fragmentation And Sleepy Devices

## Fragmentation contract

- Use FRAG for payloads that exceed frame payload capacity.
- Reassembly key includes origin and fragment group identity.
- Reassembly timeout and buffer bounds must be enforced.

## Sleepy device model

- Sleepy endpoints are pull-oriented for command retrieval.
- WAKE broadcast indicates availability.
- PULL and PULL_R provide deferred command delivery.

## OPEN DECISIONS

- Queue retention and expiration policy for pending commands.
- Fragment retry window policy under high-loss conditions.

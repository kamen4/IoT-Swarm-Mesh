# Glossary Alignment

This file locks terminology to Protocol/_docs_v1.0/00-glossary.md and related references.

## Preserved Core Terms

- q_up: UP path quality signal.
- q_total: aggregated routing centrality signal.
- q_forward: minimum threshold for DOWN forwarding eligibility.
- parent: selected upstream node in DOWN tree.
- child: node that selected current node as parent.
- decay epoch: periodic attenuation cycle for charge values.

## Preserved Envelope Fields

- ROUTING_HEADER: ver, ttl, prevHopMac, charge, decayEpochHint
- SECURE_HEADER: dir, msgType, originMac, dstMac, msgId, seq

## Preserved Lifecycle States

- Pending
- Verified
- Connected

## Naming Rules

- Do not rename protocol fields in implementation docs.
- Do not alias message types with local synonyms in normative sections.
- If local aliases are needed for code readability, keep original names in mapping tables.

## Source Pointers

- Protocol/_docs_v1.0/00-glossary.md

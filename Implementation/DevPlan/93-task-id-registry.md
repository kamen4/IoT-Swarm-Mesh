# Task ID Registry

This file reserves task ID ranges and tracks current usage for Wave1.

## Reserved ranges

| Prefix | Range | Area |
| --- | --- | --- |
| W1-FND | 001-099 | firmware foundation |
| W1-FND | 100-199 | gateway foundation |
| W1-FND | 200-299 | hub foundation |
| W1-PRO | 001-099 | secure onboarding |
| W1-PRO | 100-199 | UP routing |
| W1-PRO | 200-299 | DOWN routing |
| W1-PAR | 001-099 | baseline parameters |
| W1-INT | 100-199 | integration harness |
| W1-INT | 200-299 | observability and closure |
| FP-ARCH | 001-099 | full-product architecture |
| FP-ESP | 001-099 | device library and firmware |
| FP-GW | 001-099 | gateway bridge runtime |
| FP-HUB | 001-099 | dockerized server stack |
| FP-DATA | 001-099 | SQL and TSDB persistence |
| FP-BOT | 001-099 | telegram control plane |
| FP-OBS | 001-099 | observability dashboards |
| FP-QA | 001-099 | validation and test harness |
| FP-REL | 001-099 | release and deployment |

## Current usage snapshot

- W1-FND used: 001-018, 101-114, 201-216
- W1-PRO used: 001-015, 101-115, 201-217
- W1-PAR used: 001-014
- W1-INT used: 101-118, 201-214

## Rules

- Do not reuse existing IDs.
- Do not rename IDs after external references are added.
- If IDs are deprecated, keep a note in audit log instead of silent removal.

## Source Pointers

- Implementation/DevPlan/00-index.md
- Implementation/DevPlan/Wave1/03-integration/03-dependency-map.md
- Implementation/DevPlan/92-audit-log.md

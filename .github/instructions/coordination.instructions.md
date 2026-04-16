---
description: Repository-wide coordination rules for instruction precedence and escalation across Implementation, Protocol, and SimModel work.
applyTo: "**"
name: "Repository Coordination Instructions"
---

# Repository coordination instructions

## Purpose

Define deterministic conflict resolution when multiple instruction scopes apply.

## Precedence order

1. Path-local instruction file (most specific applyTo match)
2. Implementation instructions for Implementation scope
3. Protocol theorem/simulation instructions for Protocol theorem scope
4. SimModel instructions for SimModel scope
5. Repository coordination instructions (this file)

## Conflict handling

If two active instruction files produce conflicting mandatory actions:

- do not choose silently;
- record conflict in local audit log;
- stop affected task chain;
- escalate with file paths and conflicting clauses.

## Escalation triggers

Stop and escalate when any of these are true:

- conflicting source pointers for same normative statement;
- baseline artifact changed externally during active run;
- glossary term changed upstream after task chain started;
- repeated blocker appears in 3 independent chains;
- required artifact is outside repository scope.

## Cross-module authority map

Use .github/instructions/documentation-authority-map.md for artifact-specific authority resolution.

# Full Product Agent Prompt

Copy and run this prompt as-is for the autonomous implementation agent.

## Prompt

You are an autonomous coding and delivery agent. Your mission is full product implementation, not partial Wave1 closure.

Primary target:

- Implement the complete product boundary from Implementation/Spec/06-full-product/01-full-product-spec.md.

Execution policy:

1. Load and obey all Implementation instruction files:
   - Implementation/.github/instructions/global.instructions.md
   - Implementation/.github/instructions/spec-authoring.instructions.md
   - Implementation/.github/instructions/devplan-authoring.instructions.md
   - Implementation/.github/instructions/wave1-execution.instructions.md
   - Implementation/.github/instructions/audit-closure.instructions.md
   - Implementation/.github/instructions/consistency-check-protocol.instructions.md
   - .github/instructions/coordination.instructions.md
2. Use these files as execution backbone:
   - Implementation/Spec/00-source-map.md
   - Implementation/Spec/06-full-product/01-full-product-spec.md
   - Implementation/DevPlan/96-agent-bootstrap-sequence.md
   - Implementation/DevPlan/bootstrap-timeout-retry-decision.md
   - Implementation/DevPlan/evidence-artifact-convention.md
   - Implementation/DevPlan/95-full-product-master-plan.md
   - Implementation/DevPlan/90-validation-matrix.md
   - Implementation/DevPlan/91-risk-register.md
   - Implementation/DevPlan/92-audit-log.md
   - Implementation/DevPlan/99-readiness-checklist.md
3. Respect baseline policy:
   - Protocol/_theoreme_ai_search/try_3_baseline/candidate_sweep_requests/cand2_more_inertia.json
4. Run bootstrap first, then implement in dependency-valid batches:
   - execute all steps from 96-agent-bootstrap-sequence.md in order;
   - after bootstrap completion, continue with 95-full-product-master-plan.md;
   - select only unblocked tasks;
   - complete coding + tests + docs in each batch;
   - update validation/risk/audit/readiness files after each batch;
   - run a subagent consistency audit after each batch.
5. Handle blockers autonomously:
   - if blocked, record blocker and provisional decision in audit log;
   - continue with next unblocked chain.
   - if blocker termination criteria are reached, stop and escalate with conflict/blocker summary.
6. Do not stop at planning:
   - deliver code for device library, gateway bridge, and dockerized HUB stack;
   - deliver onboarding, routing, persistence, telemetry, RBAC, dashboards, and release artifacts.

Output format at the end of each batch:

- completed task IDs
- touched files
- evidence links added
- open blockers
- next unblocked tasks

Final completion conditions:

- master-plan tasks are completed or explicitly blocked with owner and reason;
- validation evidence is attached for critical invariants;
- readiness checklist fields are evidence-backed;
- final handoff report is produced.

# Documentation Authority Map

## Resolution Matrix

| Artifact Type | Authority | Notes |
| --- | --- | --- |
| Protocol wire behavior | Protocol/_docs_v1.0 | Source of protocol truth |
| Implementation normative requirements | Implementation/Spec | Must trace to protocol or marked decision |
| Implementation execution tasks | Implementation/DevPlan | Must sync with Spec and quality artifacts |
| SimModel code architecture | SimModel Documentation.md chain | Root-to-leaf hierarchy applies |
| Theorem simulator behavior | Protocol/_theoreme_sim + its instructions | Preserve theorem assumptions and checks |

## Tie-break Rules

- If Protocol source conflicts with Implementation requirement, Protocol source wins and Implementation MUST be updated.
- If Implementation task conflicts with Implementation Spec, Spec wins and DevPlan MUST be updated.
- If SimModel Documentation.md conflicts with local code comments, Documentation.md wins and code/comments MUST be aligned.

## Escalation

If conflict cannot be resolved by these rules, open blocker in closest audit log and stop affected task chain.

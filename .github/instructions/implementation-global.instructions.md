---
description: Load these instructions for any task in the Implementation folder, including specification updates, development plan maintenance, reviews, refactors, and structure changes.
applyTo: "Implementation/**/*"
---

Use these instructions as the default policy for all work in the Implementation scope when creating, updating, reviewing, or restructuring planning and specification documents.

# Implementation global working instructions

## Language

Use English for all documentation and comments in the Implementation scope, even if some source materials are in other languages. This ensures maximum accessibility for all current and future contributors.

## Core principles

1. Documentation is the single source of truth for Implementation scope.
2. Implementation documents must align with protocol documentation and must not contradict it.
3. Keep documents clean, low-noise, and easy to navigate.
4. Prefer many small, well-named folders and many small, well-named files over large mixed structures.
5. Every folder in Implementation must have a `Docs.md` file that is the source of truth for that folder.
6. When changing documents, update related index and traceability files in the same task.

## Documentation authority and synchronization

### Source of truth

- Treat files in `Implementation/Spec` and `Implementation/DevPlan` as authoritative for implementation planning.
- Treat `Protocol/_docs_v1.0` as the authoritative protocol source and keep Implementation documents consistent with it.
- If Implementation documents and protocol documents disagree, resolve the mismatch before finishing.
- Do not leave important decisions undocumented.

### Required documentation hierarchy

- Every folder under `Implementation` must contain exactly one `Docs.md` file.
- The local `Docs.md` is the source of truth for that folder.
- Each `Docs.md` must describe, at minimum:
  - folder purpose and boundary;
  - subfolders inside that folder and why they exist;
  - files inside that folder and their responsibilities;
  - important interactions, constraints, and extension points at that folder level;
  - relation to the parent folder and to the overall Implementation flow.
- Each major folder under `Implementation/Spec` and `Implementation/DevPlan` must have clear purpose and boundary.
- Each folder-level document set must describe, at minimum:
  - folder purpose and boundary;
  - contained files and responsibilities;
  - important interactions, constraints, and extension points;
  - relation to parent folder and overall Implementation flow.
- Documentation must be readable from top-level index to detailed files without guesswork.

### Update propagation

- Any non-trivial document change must update all affected navigation and traceability documents.
- When adding, moving, renaming, or deleting files, reflect the change in relevant index files and cross-references.
- When adding, moving, renaming, or deleting files or folders, update the local `Docs.md` and all affected parent `Docs.md` files.
- When behavior or decision changes, update:
  - the local target file;
  - the local `Docs.md`;
  - the relevant plan/spec summary file;
  - any affected decision pack or open-question tracker.

## Structure expectations

### General structure rules

- Avoid crowded folders that mix unrelated concerns.
- Group by responsibility and domain boundary.
- Keep naming consistent and placement predictable.
- Keep folder layout stable to preserve link integrity.

### Restructuring preference

- For larger updates, prefer improving structure rather than appending content to overloaded files.
- Do not create unnecessary folders for trivial cases.
- When a concern grows across multiple files, move it to a dedicated folder section.

## File-level documentation rules

### Markdown and planning files

- Every file must clearly state purpose, scope, and expected outputs.
- Use concise sections, explicit checklists, and clear acceptance criteria where relevant.
- Keep OPEN DECISION items clearly labeled and easy to track.
- Do not present assumptions as facts when source docs do not define them.

### Commenting requirements

- Write concise, meaningful comments for non-trivial logic, decisions, and constraints.
- In Markdown files, use short explanatory notes where intent is not obvious.
- In code or config-like files, add format-appropriate comments for non-obvious behavior.
- Do not add placeholder comments or comments that only restate obvious syntax.

### Non-Markdown files in Implementation scope

- If non-Markdown files are added, include format-appropriate explanatory comments where supported.
- If comments are not supported by the format, document behavior in the nearest related Markdown file.

## Character and encoding rules

Use plain ASCII characters only throughout the repository:

- Allowed: letters A-Z a-z, digits 0-9, standard punctuation (`! " # $ % & ' ( ) * + , - . / : ; < = > ? @ [ \ ] ^ _ `` { | } ~`), space, CR, LF, and tab.
- Forbidden: em dashes, en dashes, curly quotes, ellipsis, Unicode arrows, box-drawing characters, math symbols, multiplication sign, superscripts, middle dot, guillemets, checkmarks, smileys, and any non-ASCII codepoint.

Concrete substitutions:

- `--` or `-` instead of em dash or en dash
- `-` instead of Unicode minus
- `->` and `<-` instead of Unicode arrows
- `<=` and `>=` instead of Unicode comparison symbols
- `O(n^2)` instead of superscript notation
- `x` instead of multiplication sign
- `*` or `(yes)` instead of checkmarks
- ASCII tree styles instead of box-drawing characters

## Cleanliness and quality bar

- Keep documents compact, structured, and easy to scan.
- Avoid duplicated requirements across unrelated files.
- Prefer explicit, short statements over long narrative blocks.
- Keep terminology consistent across Spec and DevPlan.
- Avoid stale or orphaned references.

## Change workflow expectations

- For each non-trivial change, review impacted files in the local chain before finishing.
- Ensure new files are reflected in relevant index files.
- Ensure renamed or moved content is removed from old references and added to new references.
- Keep traceability and decision-tracking files synchronized with current content.

## Large-task execution guidance

- Break large Implementation tasks into bounded subproblems by folder and concern.
- Use subagents for broad multi-folder analysis tasks.
- Use subagents for consistency checks across Spec, DevPlan, traceability files, and `Docs.md` hierarchy.
- For any medium or large task, use subagents as the default approach for analysis and verification.
- Consolidate parallel findings into coherent root-to-leaf documentation updates.

## Subagent usage policy

- Use subagents whenever the task touches multiple folders or multiple documentation layers.
- Use subagents to verify structural consistency after non-trivial updates.
- Prefer parallel subagent checks for large audits and large restructuring tasks.

## Decision rules for AI changes

- Do not add implementation assumptions without labeling them as OPEN DECISION when sources do not define them.
- Do not add major files without updating related index and navigation files.
- Do not leave outdated references in plan/spec documents.
- Prefer changes that improve both implementation clarity and document discoverability.
---
description: Load these instructions for any task in the SimModel solution, including feature work, refactors, reviews, bug fixes, documentation maintenance, and repository structure changes.
applyTo: "SimModel/**/*"
---

Use these instructions as the default repository policy for the entire SimModel solution when generating code, answering questions, reviewing changes, and restructuring projects or folders.

# SimModel global working instructions

## Core principles

1. Documentation is the single source of truth.
2. Code, XML documentation, inline comments, and generated docs must reflect the documentation hierarchy and must never contradict it.
3. Keep the solution clean, low-noise, and easy to navigate.
4. Prefer small, well-named folders over large mixed folders.
5. When changing code, update the documentation in the same task.

## Documentation authority and synchronization

### Source of truth

- Treat `Documentation.md` files as the authoritative description of architecture, intent, structure, responsibilities, and behavior.
- Generated documentation, XML summaries, and inline comments must be derived from and aligned with the relevant `Documentation.md` files.
- If code and documentation disagree, fix both so they match before finishing the task.
- Do not leave behavior undocumented or documented in only one place.

### Required documentation hierarchy

- Every folder depth level must contain exactly one `Documentation.md` file for that folder.
- Each folder-level `Documentation.md` must describe, at minimum:
  - the folder purpose and boundary;
  - the subfolders it contains and why they exist;
  - the files it contains and their responsibilities;
  - important interactions, constraints, and extension points relevant at that level;
  - how this folder fits into its parent and the overall solution.
- This rule applies to the root folder, each project folder, and every meaningful nested folder.
- Documentation must be readable from root to leaf. A reader should be able to start at the root `Documentation.md` and drill down folder by folder without guesswork.

### Update propagation

- Any code change must update documentation at every affected level, from the changed file's folder up to the repository root.
- Update only the documentation levels affected by the change, but do not skip any affected ancestor folder.
- When adding, moving, renaming, or deleting files or folders, reflect that change in all impacted `Documentation.md` files.
- When behavior changes, update both the local folder documentation and any higher-level architectural documentation that references that behavior.

## Project structure expectations

### General structure rules

- Avoid crowded folders that mix unrelated concerns.
- Prefer grouping by responsibility and domain boundary.
- Keep top-level folders minimal and intentional.
- Use consistent naming and predictable placement so related items are easy to find.

### Folder organization guidance

- Contracts such as interfaces and abstractions should live in dedicated folders when they represent a distinct concern.
- Models and DTO-like types with shared meaning should live in dedicated folders.
- Network builders, network topologies, packets, routers, statistics, and similar independent concepts should live in separate folders.
- Each independent router implementation should have its own dedicated folder when it has supporting files, configuration, policies, helpers, or strategy-specific models.
- Apply the same documentation and organization rules consistently across all projects, including `Engine` and `WebApp`, and any future projects.

### Restructuring preference

- When implementing larger changes, prefer improving structure instead of adding more files into already overloaded folders.
- Do not create unnecessary folders for trivial cases, but once a concern has multiple files or a clear boundary, give it a dedicated folder.
- Optimize for discoverability, maintainability, and documentation clarity.

## File-level documentation rules

### C# files

- Every `.cs` file must contain XML documentation that fully describes the logic as implemented now.
- XML documentation must be complete and useful for the current code, not placeholder text.
- Public types and members must be documented.
- Internal/private members should also be documented when the logic is non-trivial, stateful, algorithmic, or important for maintenance.
- XML documentation should explain responsibility, key behavior, invariants, important side effects, and non-obvious decisions.
- Do not use XML comments to invent behavior that the code does not implement.

### Non-C# files

- Every non-`.cs` file must include comments or explanatory text appropriate to its format, describing the logic, purpose, and important behavior.
- Examples include Markdown, Razor, JSON-like configs where supported, JavaScript, CSS, YAML, and build/config files.
- Keep comments meaningful and concise. Explain intent and important decisions, not obvious syntax.
- If a format does not support comments directly, place the explanation in the folder-level `Documentation.md` and nearby supporting documentation.

## Character and encoding rules

Use plain ASCII characters only throughout the entire repository:

- **Allowed:** letters A-Z a-z, digits 0-9, standard punctuation (`! " # $ % & ' ( ) * + , - . / : ; < = > ? @ [ \ ] ^ _ `` { | } ~`), space, CR, LF, and tab.
- **Forbidden:** em dashes (--), en dashes, curly/smart quotes, ellipsis, arrows, box-drawing characters, math symbols, multiplication sign, superscripts, middle dot, guillemets, checkmarks, smileys, and any other non-ASCII Unicode codepoint.

Concrete substitutions to apply:

- `--` or `-` instead of em dash (U+2014) or en dash (U+2013)
- `-` (hyphen-minus) instead of minus sign (U+2212)
- `->` and `<-` instead of arrow characters
- `<=` and `>=` instead of <= and >= Unicode symbols
- `O(n^2)` instead of O(n squared with superscript)
- `x` instead of multiplication sign (U+00D7)
- `*` or `(yes)` instead of checkmarks or similar symbols
- Plain text descriptions instead of box-drawing characters (ASCII tree with `|`, `+`, `-` is acceptable)
- Remove or replace any U+FFFD replacement character with the intended ASCII text

This rule applies to: Markdown files, XML documentation comments, inline code comments, config files, YAML, and all other text files in the repository.

## Cleanliness and quality bar

- Keep code and documentation clean, compact, and easy to scan.
- Avoid overloaded files, duplicated explanations, and unnecessary verbosity.
- Prefer precise summaries, short sections, and structured lists over long unbroken text.
- Maintain consistency in naming, terminology, and folder descriptions.
- Do not scatter critical knowledge across unrelated files.

## Change workflow expectations

- For every non-trivial change, review the impacted documentation chain before finishing.
- Ensure new files are mentioned in the parent folder documentation.
- Ensure moved responsibilities are removed from old documentation and added to the new location.
- Ensure XML documentation and non-C# comments remain synchronized with the authoritative folder documentation.
- If generated docs become too large or noisy, improve the source documentation structure rather than adding more ad hoc text.

## Large-task execution guidance

- When asked to perform a large task that spans multiple folders or the whole solution, break the work into subproblems by folder, project, or bounded concern.
- Create subagents for broad, multi-folder tasks so work can be analyzed in parallel across the solution.
- Use subagents especially for documentation audits, structural refactors, cross-project consistency checks, and large feature work.
- After parallel investigation, consolidate the result into a coherent root-to-leaf documentation and implementation update.

## Decision rules for AI changes

- Do not add code without deciding where its documentation belongs.
- Do not add folders without adding their `Documentation.md`.
- Do not leave outdated documentation behind.
- Do not treat generated documentation output as the main place for understanding the system.
- Prefer solutions that improve both architecture clarity and documentation discoverability.

## Expected outcome

The repository should be understandable by reading `Documentation.md` files from the root down to the relevant folder, then opening the target file and reading its XML documentation or local comments for implementation detail. No important behavior should require guesswork or dependence on oversized generated documentation output.

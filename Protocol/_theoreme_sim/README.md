# Theorem Simulation

This folder contains a static JavaScript/HTML/CSS simulator for the DOWN routing charge-induced tree theorem.

## What this simulator demonstrates

- Charge-induced parent selection on the forward-eligible subset.
- Per-round charge spread dynamics from gateway source through neighbor estimates.
- Per-round DOWN then UP execution: root emits DOWN, duplicates are tracked, then UP attempts refresh charges.
- Traffic-based link reinforcement where heavily used edges become more reliable over time.
- Periodic DECAY epochs that attenuate charges and neighbor estimates to prevent unbounded growth.
- Convergence checks for assumptions and theorem lemmas.
- DOWN tree-broadcast simulation (loop/duplicate behavior).
- Headless "No UI Simulation" execution with JSON/CSV export snapshots.
- Batch research block for topology x parameter sweeps with standalone HTML report export and in-report JSON/CSV download buttons.
- Compact PASS diagnostics (assumptions/theorem/lemmas) in research reports without heavy payload growth.
- Graph rendering with edge thickness and brightness driven by charge weights.

## Scope note

This simulator verifies structural theorem assumptions and lemmas while charges evolve round by round.
It intentionally does not emulate low-level radio behavior, full UP swarm packet flow, or full-frame TTL/dedup transport timing.
Early rounds can show PENDING theorem status while only a trivial eligible subset exists.
Long-run stability is controlled by hysteresis, link reinforcement, and configurable DECAY scheduling.

## Structure

- src/ - Simulation logic split into small focused modules.
- styles/ - Visual theme and layout.
- index.html - App entry point.

## Run

Open index.html in a modern browser.

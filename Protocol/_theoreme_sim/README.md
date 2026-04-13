# Theorem Simulation

This folder contains a static JavaScript/HTML/CSS simulator for the DOWN routing charge-induced tree theorem.

## What this simulator demonstrates

- Charge-induced parent selection on the forward-eligible subset.
- Convergence checks for assumptions and theorem lemmas.
- DOWN tree-broadcast simulation (loop/duplicate behavior).
- Graph rendering with edge thickness and brightness driven by charge weights.

## Scope note

This simulator verifies the structural theorem assumptions and lemmas on a stable charge field.
It intentionally does not emulate low-level radio behavior, full UP swarm packet flow, or full-frame TTL/dedup transport timing.

## Structure

- src/ - Simulation logic split into small focused modules.
- styles/ - Visual theme and layout.
- index.html - App entry point.

## Run

Open index.html in a modern browser.

/* Purpose: Render simulation controls and map user input to simulation configuration updates. */

import { DEFAULT_CONFIG } from "../core/constants.js";

export class ControlPanel {
  /**
   * @param {HTMLElement} container
   * @param {(config:any) => void} onGenerate
   * @param {() => void} onStep
   * @param {() => void} onToggleRun
   * @param {(config:any) => void} onReset
   * @param {() => void} onBroadcast
   */
  constructor(
    container,
    onGenerate,
    onStep,
    onToggleRun,
    onReset,
    onBroadcast,
  ) {
    this.container = container;
    this.onGenerate = onGenerate;
    this.onStep = onStep;
    this.onToggleRun = onToggleRun;
    this.onReset = onReset;
    this.onBroadcast = onBroadcast;
    this.isRunning = false;
    this.render(DEFAULT_CONFIG);
  }

  /**
   * @param {any} config
   */
  render(config) {
    this.container.innerHTML = `
      <div class="group" title="Topology and random generation settings.">
        <h3 title="Settings that define graph size and geometry.">Topology</h3>
        ${numberControl("nodeCount", "Nodes", config.nodeCount, 8, 180, 1, "Number of devices generated in the mesh graph.")}
        ${numberControl("linkRadius", "Link radius", config.linkRadius, 60, 360, 1, "Maximum geometric distance used to create links.")}
        ${numberControl("seed", "Seed", config.seed, 1, 999999, 1, "Deterministic random seed for reproducible graph generation.")}
      </div>

      <div class="group" title="Parameters that influence theorem checks and parent selection.">
        <h3 title="Mathematical parameters for eligibility and routing constraints.">Theorem Parameters</h3>
        ${numberControl("qForward", "q_forward", config.qForward, 20, 1300, 1, "Eligibility threshold. Nodes below this q_total are excluded from forwarding.")}
        ${rangeControl("deliveryProbability", "Delivery probability", config.deliveryProbability, 0.15, 1, 0.01, "Probability that neighbor charge advertisement is delivered in each round.")}
        ${numberControl("penaltyLambda", "Penalty lambda", config.penaltyLambda, 0, 150, 1, "Weight of link-quality penalty in near-tie parent decisions.")}
        ${numberControl("switchHysteresis", "Switch hysteresis", config.switchHysteresis, 0, 120, 1, "Extra margin needed before switching to a new parent.")}
        ${checkboxControl("enforceTheoremAssumptions", "Enforce A5/A6 at init", config.enforceTheoremAssumptions, "When enabled, initial charges are repaired to satisfy theorem assumptions A5 and A6.")}
      </div>

      <div class="group" title="Simulation execution controls.">
        <h3 title="Controls for stepping, auto-running, and broadcast tests.">Run</h3>
        ${numberControl("roundsPerSecond", "Rounds/sec", config.roundsPerSecond, 1, 30, 1, "Execution speed during automatic run mode.")}
        ${numberControl("maxRounds", "Max rounds", config.maxRounds, 10, 3000, 1, "Hard limit after which automatic run stops.")}
        <div class="control"><button data-action="generate" class="primary" title="Rebuild topology and reset state using current parameters.">Generate Topology</button></div>
        <div class="control"><button data-action="step" title="Execute one simulation round and refresh theorem checks.">Step Round</button></div>
        <div class="control"><button data-action="run" title="Start or pause continuous simulation rounds.">Run / Pause</button></div>
        <div class="control"><button data-action="broadcast" class="warn" title="Simulate DOWN message propagation on current parent tree.">Simulate DOWN Broadcast</button></div>
        <div class="control"><button data-action="reset" title="Reset simulation by recreating state with current controls.">Reset Round Counter</button></div>
      </div>
    `;

    this.bindEvents();
    this.setRunning(this.isRunning);
  }

  bindEvents() {
    this.container
      .querySelector("[data-action='generate']")
      .addEventListener("click", () => {
        this.onGenerate(this.readConfig());
      });

    this.container
      .querySelector("[data-action='step']")
      .addEventListener("click", () => {
        this.onStep();
      });

    this.container
      .querySelector("[data-action='run']")
      .addEventListener("click", () => {
        this.onToggleRun();
      });

    this.container
      .querySelector("[data-action='broadcast']")
      .addEventListener("click", () => {
        this.onBroadcast();
      });

    this.container
      .querySelector("[data-action='reset']")
      .addEventListener("click", () => {
        this.onReset(this.readConfig());
      });
  }

  /**
   * @returns {any}
   */
  readConfig() {
    const readNumber = (name) =>
      Number(this.container.querySelector(`[name='${name}']`).value);
    const readCheckbox = (name) =>
      this.container.querySelector(`[name='${name}']`).checked;

    return {
      nodeCount: readNumber("nodeCount"),
      linkRadius: readNumber("linkRadius"),
      qForward: readNumber("qForward"),
      deliveryProbability: readNumber("deliveryProbability"),
      penaltyLambda: readNumber("penaltyLambda"),
      switchHysteresis: readNumber("switchHysteresis"),
      seed: readNumber("seed"),
      roundsPerSecond: readNumber("roundsPerSecond"),
      maxRounds: readNumber("maxRounds"),
      enforceTheoremAssumptions: readCheckbox("enforceTheoremAssumptions"),
    };
  }

  /**
   * @param {boolean} running
   */
  setRunning(running) {
    this.isRunning = running;
    const button = this.container.querySelector("[data-action='run']");
    button.textContent = running ? "Pause" : "Run";
    button.className = running ? "warn" : "";
    button.title = running
      ? "Pause continuous simulation rounds."
      : "Start continuous simulation rounds.";
  }
}

function numberControl(name, label, value, min, max, step, tooltip) {
  const safeTooltip = escapeAttr(tooltip);
  return `
    <div class="control" title="${safeTooltip}">
      <label for="${name}" title="${safeTooltip}">${label}</label>
      <input id="${name}" name="${name}" type="number" value="${value}" min="${min}" max="${max}" step="${step}" title="${safeTooltip}">
    </div>
  `;
}

function rangeControl(name, label, value, min, max, step, tooltip) {
  const safeTooltip = escapeAttr(tooltip);
  return `
    <div class="control" title="${safeTooltip}">
      <label for="${name}" title="${safeTooltip}">${label}: <span class="small-mono">${value}</span></label>
      <input id="${name}" name="${name}" type="range" value="${value}" min="${min}" max="${max}" step="${step}" title="${safeTooltip}">
    </div>
  `;
}

function checkboxControl(name, label, checked, tooltip) {
  const safeTooltip = escapeAttr(tooltip);
  return `
    <div class="control" title="${safeTooltip}">
      <label title="${safeTooltip}">
        <input name="${name}" type="checkbox" ${checked ? "checked" : ""} title="${safeTooltip}"> ${label}
      </label>
    </div>
  `;
}

function escapeAttr(value) {
  return String(value)
    .replace(/&/g, "&amp;")
    .replace(/"/g, "&quot;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;");
}

/* Purpose: Render simulation controls and map user input to simulation configuration updates. */

import { randomizeAllConfig } from "../config/randomConfigGenerator.js";
import { normalizeConfig } from "../config/configNormalizer.js";
import { DEFAULT_CONFIG } from "../core/constants.js";
import { captureDetailsState, resolveOpenState } from "./collapsibleGroups.js";
import { bindLiveRangeValue } from "./liveRangeValue.js";

export class ControlPanel {
  /**
   * @param {HTMLElement} container
   * @param {(config:any) => void} onGenerate
   * @param {() => void} onStep
   * @param {() => void} onToggleRun
   * @param {(config:any) => void} onReset
   * @param {() => void} onBroadcast
   * @param {(patch:Record<string,any>) => void} onLiveConfigChange
   * @param {(config:any,maxSteps:number) => void} onNoUiSimulation
   * @param {(request:any) => void|Promise<any>} onResearchBatch
   */
  constructor(
    container,
    onGenerate,
    onStep,
    onToggleRun,
    onReset,
    onBroadcast,
    onLiveConfigChange,
    onNoUiSimulation,
    onResearchBatch,
  ) {
    this.container = container;
    this.onGenerate = onGenerate;
    this.onStep = onStep;
    this.onToggleRun = onToggleRun;
    this.onReset = onReset;
    this.onBroadcast = onBroadcast;
    this.onLiveConfigChange = onLiveConfigChange;
    this.onNoUiSimulation = onNoUiSimulation;
    this.onResearchBatch = onResearchBatch;
    this.isRunning = false;
    this.panelCollapsed = false;
    this.groupOpenState = {
      topology: true,
      theorem: true,
      stability: true,
      run: true,
      research: true,
    };
    this.currentConfig = { ...DEFAULT_CONFIG };
    this.disposeLiveListeners = [];
    this.researchProgress = createInitialResearchProgress();
    this.researchHideTimeout = null;

    this.render(DEFAULT_CONFIG);
  }

  /**
   * @param {any} config
   */
  render(config) {
    const previousState = captureDetailsState(this.container);
    this.groupOpenState = { ...this.groupOpenState, ...previousState };
    this.currentConfig = normalizeConfig(config);

    this.disposeLiveListeners.forEach((dispose) => dispose());
    this.disposeLiveListeners = [];

    this.container.classList.toggle("panel-collapsed", this.panelCollapsed);

    this.container.innerHTML = `
      ${researchProgressWindow(this.researchProgress)}

      <div class="panel-toolbar" title="Collapse or expand the full settings panel.">
        <button data-action="toggle-panel" title="Collapse/expand settings panel.">${this.panelCollapsed ? "Expand settings" : "Collapse settings"}</button>
      </div>

      <div class="panel-content ${this.panelCollapsed ? "is-hidden" : ""}">
        <details class="group group-collapsible" data-group-id="topology" ${openAttr(resolveOpenState(this.groupOpenState, "topology", true))} title="Topology and random generation settings.">
          <summary class="group-summary" title="Settings that define graph size and geometry.">Topology</summary>
          <div class="group-inner">
            ${numberControl("nodeCount", "Nodes", config.nodeCount, 8, 180, 1, "Number of devices generated in the mesh graph.")}
            ${numberControl("linkRadius", "Link radius", config.linkRadius, 60, 360, 1, "Maximum geometric distance used to create links.")}
            ${numberControl("seed", "Seed", config.seed, 1, 999999, 1, "Deterministic random seed for reproducible graph generation.")}
            <div class="control"><button data-action="randomize" title="Generate a full random set of simulation parameters.">Randomize All Params</button></div>
          </div>
        </details>

        <details class="group group-collapsible" data-group-id="theorem" ${openAttr(resolveOpenState(this.groupOpenState, "theorem", true))} title="Parameters that influence theorem checks and parent selection.">
          <summary class="group-summary" title="Mathematical parameters for eligibility and routing constraints.">Theorem Parameters</summary>
          <div class="group-inner">
            ${numberControl("qForward", "q_forward", config.qForward, 20, 1300, 1, "Eligibility threshold. Nodes below this q_total are excluded from forwarding.")}
            ${rangeControl("deliveryProbability", "Delivery probability", config.deliveryProbability, 0.15, 1, 0.01, "Probability that neighbor charge advertisement is delivered in each round.")}
            ${numberControl("penaltyLambda", "Penalty lambda", config.penaltyLambda, 0, 150, 1, "Weight of link-quality penalty in near-tie parent decisions.")}
            ${numberControl("switchHysteresis", "Switch hysteresis", config.switchHysteresis, 0, 120, 1, "Extra margin needed before switching to a new parent.")}
            ${rangeControl("switchHysteresisRatio", "Switch hysteresis ratio", config.switchHysteresisRatio, 0, 0.2, 0.01, "Relative hysteresis against estimate scale to suppress parent flapping.")}
            ${numberControl("rootSourceCharge", "Root source charge", config.rootSourceCharge, 500, 2200, 1, "Persistent source charge emitted by gateway for network-wide spread.")}
            ${numberControl("chargeDropPerHop", "Charge drop per hop", config.chargeDropPerHop, 10, 260, 1, "Charge attenuation from best neighbor estimate to local node target.")}
            ${rangeControl("chargeSpreadFactor", "Charge spread factor", config.chargeSpreadFactor, 0.05, 1, 0.01, "Fraction of gap to target charge closed each round.")}
            ${checkboxControl("enforceTheoremAssumptions", "Enforce A5/A6 at init", config.enforceTheoremAssumptions, "Keeps optional assumption bootstrap flag in config for experiments.")}
          </div>
        </details>

        <details class="group group-collapsible" data-group-id="stability" ${openAttr(resolveOpenState(this.groupOpenState, "stability", true))} title="Convergence and long-run stability parameters.">
          <summary class="group-summary" title="Decay and link reinforcement controls for oscillation prevention.">Stability and Decay</summary>
          <div class="group-inner">
            ${numberControl("decayIntervalSteps", "Decay interval (steps)", config.decayIntervalSteps, 0, 500, 1, "How often to trigger DECAY epoch (0 disables decay).")}
            ${rangeControl("decayPercent", "Decay percent", config.decayPercent, 0, 0.4, 0.01, "When decay triggers, charges and estimates are multiplied by (1 - decayPercent).")}
            ${rangeControl("linkMemory", "Link memory", config.linkMemory, 0.75, 0.995, 0.001, "How long link-usage history is retained while strengthening frequently used links.")}
            ${rangeControl("linkLearningRate", "Link learning rate", config.linkLearningRate, 0.05, 0.8, 0.01, "How quickly effective link quality reacts to observed traffic.")}
            ${numberControl("linkBonusMax", "Link bonus max", config.linkBonusMax, 0, 120, 1, "Maximum parent-score bonus for stable high-traffic links.")}
          </div>
        </details>

        <details class="group group-collapsible" data-group-id="run" ${openAttr(resolveOpenState(this.groupOpenState, "run", true))} title="Simulation execution controls.">
          <summary class="group-summary" title="Controls for stepping, auto-running, headless runs, and broadcast tests.">Run</summary>
          <div class="group-inner">
            ${numberControl("roundsPerSecond", "Rounds/sec", config.roundsPerSecond, 1, 30, 1, "Execution speed during automatic run mode.")}
            ${numberControl("maxRounds", "Max rounds", config.maxRounds, 20, 5000, 1, "Maximum step count for run and No UI simulation.")}
            <div class="control"><button data-action="generate" class="primary" title="Rebuild topology and reset state using current parameters.">Generate Topology</button></div>
            <div class="control"><button data-action="step" title="Execute one simulation round and refresh theorem checks.">Step Round</button></div>
            <div class="control"><button data-action="run" title="Start or pause continuous simulation rounds.">Run / Pause</button></div>
            <div class="control"><button data-action="broadcast" class="warn" title="Simulate DOWN message propagation on current parent tree.">Simulate DOWN Broadcast</button></div>
            <div class="control"><button data-action="no-ui" title="Run headless simulation for max rounds and export JSON/CSV.">No UI Simulation</button></div>
            <div class="control"><button data-action="reset" title="Reset simulation by recreating state with current controls.">Reset Round Counter</button></div>
          </div>
        </details>

        <details class="group group-collapsible" data-group-id="research" ${openAttr(resolveOpenState(this.groupOpenState, "research", true))} title="Batch research runner for topology and parameter stability studies.">
          <summary class="group-summary" title="Headless topology x parameter experiments with HTML report export.">Research Batch</summary>
          <div class="group-inner">
            ${numberControl("researchSeedStart", "Seed start", config.seed, 1, 9999999, 1, "First deterministic seed used for batch experiments.")}
            ${numberControl("researchSeedCount", "Seeds per check", 3, 1, 20, 1, "How many consecutive seeds to test for each topology and parameter vector.")}
            ${numberControl("researchRoundsPerCheck", "Rounds per check", Math.max(config.maxRounds, 350), 20, 20000, 1, "Round budget for each individual stability check.")}
            ${numberControl("researchOptimizationIterations", "Optimization iterations", 12, 3, 40, 1, "Iterations of adaptive vector search per topology. More iterations improve quality but increase run time.")}
            ${textAreaControl("researchMatrixText", "Topology matrix (optional)", "", 4, "Optional explicit topology pairs in format NxR, for example: 24x150; 40x190; 72x240.", "24x150; 40x190; 72x240")}
            <div class="control small-mono" title="If matrix text is empty, automatic ranges are used.">If matrix is empty, ranges below define topology grid.</div>
            ${numberControl("researchNodeMin", "Node min", Math.max(8, config.nodeCount - 12), 8, 320, 1, "Minimum node count for generated topology range.")}
            ${numberControl("researchNodeMax", "Node max", config.nodeCount + 12, 8, 320, 1, "Maximum node count for generated topology range.")}
            ${numberControl("researchNodeStep", "Node step", 8, 1, 120, 1, "Step size for node-count topology range.")}
            ${numberControl("researchRadiusMin", "Radius min", Math.max(60, config.linkRadius - 30), 40, 600, 1, "Minimum link radius for generated topology range.")}
            ${numberControl("researchRadiusMax", "Radius max", config.linkRadius + 30, 40, 600, 1, "Maximum link radius for generated topology range.")}
            ${numberControl("researchRadiusStep", "Radius step", 20, 1, 200, 1, "Step size for link-radius topology range.")}
            <div class="control"><button data-action="research-batch" class="primary" title="Run full batch study and export HTML report with embedded JSON/CSV download buttons." ${this.researchProgress.running ? "disabled" : ""}>${this.researchProgress.running ? "Research running..." : "Run Research Batch"}</button></div>
          </div>
        </details>
      </div>
    `;

    this.bindEvents();
    this.bindLiveRangeHooks();
    this.setRunning(this.isRunning);
    this.syncResearchProgressUi();
  }

  bindEvents() {
    this.container
      .querySelector("[data-action='toggle-panel']")
      .addEventListener("click", () => {
        this.groupOpenState = {
          ...this.groupOpenState,
          ...captureDetailsState(this.container),
        };
        this.panelCollapsed = !this.panelCollapsed;
        this.render(this.readConfig());
      });

    const generate = this.container.querySelector("[data-action='generate']");
    if (generate) {
      generate.addEventListener("click", () => {
        this.onGenerate(this.readConfig());
      });
    }

    const randomize = this.container.querySelector("[data-action='randomize']");
    if (randomize) {
      randomize.addEventListener("click", () => {
        const randomConfig = randomizeAllConfig(this.readConfig());
        this.render(randomConfig);
        this.onGenerate(randomConfig);
      });
    }

    const step = this.container.querySelector("[data-action='step']");
    if (step) {
      step.addEventListener("click", () => {
        this.onStep();
      });
    }

    const run = this.container.querySelector("[data-action='run']");
    if (run) {
      run.addEventListener("click", () => {
        this.onToggleRun();
      });
    }

    const broadcast = this.container.querySelector("[data-action='broadcast']");
    if (broadcast) {
      broadcast.addEventListener("click", () => {
        this.onBroadcast();
      });
    }

    const noUi = this.container.querySelector("[data-action='no-ui']");
    if (noUi) {
      noUi.addEventListener("click", () => {
        const config = this.readConfig();
        this.onNoUiSimulation(config, config.maxRounds);
      });
    }

    const reset = this.container.querySelector("[data-action='reset']");
    if (reset) {
      reset.addEventListener("click", () => {
        this.onReset(this.readConfig());
      });
    }

    const researchBatch = this.container.querySelector(
      "[data-action='research-batch']",
    );
    if (researchBatch) {
      researchBatch.addEventListener("click", async () => {
        if (this.researchProgress.running) {
          return;
        }

        this.startResearchProgress();

        if (typeof this.onResearchBatch === "function") {
          try {
            const request = this.readResearchRequest();
            await Promise.resolve(
              this.onResearchBatch({
                ...request,
                yieldEveryRuns: 1,
                onProgress: (info) => {
                  this.updateResearchProgress(info);
                },
              }),
            );
            this.finishResearchProgress(false);
          } catch (error) {
            const message =
              error instanceof Error ? error.message : String(error);
            this.finishResearchProgress(true, message);
          }
        } else {
          this.finishResearchProgress(
            true,
            "Research callback is not configured.",
          );
        }
      });
    }
  }

  bindLiveRangeHooks() {
    if (this.panelCollapsed) {
      return;
    }

    const deliveryDispose = bindLiveRangeValue(
      this.container,
      "deliveryProbability",
      (value) => {
        this.onLiveConfigChange?.({ deliveryProbability: value });
      },
    );

    const spreadDispose = bindLiveRangeValue(
      this.container,
      "chargeSpreadFactor",
      (value) => {
        this.onLiveConfigChange?.({ chargeSpreadFactor: value });
      },
    );

    const decayPercentDispose = bindLiveRangeValue(
      this.container,
      "decayPercent",
      (value) => {
        this.onLiveConfigChange?.({ decayPercent: value });
      },
    );

    const hysteresisRatioDispose = bindLiveRangeValue(
      this.container,
      "switchHysteresisRatio",
      (value) => {
        this.onLiveConfigChange?.({ switchHysteresisRatio: value });
      },
    );

    const linkMemoryDispose = bindLiveRangeValue(
      this.container,
      "linkMemory",
      (value) => {
        this.onLiveConfigChange?.({ linkMemory: value });
      },
    );

    const linkLearningDispose = bindLiveRangeValue(
      this.container,
      "linkLearningRate",
      (value) => {
        this.onLiveConfigChange?.({ linkLearningRate: value });
      },
    );

    this.disposeLiveListeners.push(
      deliveryDispose,
      spreadDispose,
      decayPercentDispose,
      hysteresisRatioDispose,
      linkMemoryDispose,
      linkLearningDispose,
    );
  }

  /**
   * @returns {any}
   */
  readConfig() {
    if (this.panelCollapsed) {
      return { ...this.currentConfig };
    }

    const readNumber = (name) =>
      Number(
        this.container.querySelector(`[name='${name}']`)?.value ??
          this.currentConfig[name],
      );
    const readCheckbox = (name) =>
      this.container.querySelector(`[name='${name}']`)?.checked ??
      this.currentConfig[name];

    const config = {
      nodeCount: readNumber("nodeCount"),
      linkRadius: readNumber("linkRadius"),
      qForward: readNumber("qForward"),
      deliveryProbability: readNumber("deliveryProbability"),
      penaltyLambda: readNumber("penaltyLambda"),
      switchHysteresis: readNumber("switchHysteresis"),
      switchHysteresisRatio: readNumber("switchHysteresisRatio"),
      rootSourceCharge: readNumber("rootSourceCharge"),
      chargeDropPerHop: readNumber("chargeDropPerHop"),
      chargeSpreadFactor: readNumber("chargeSpreadFactor"),
      decayIntervalSteps: readNumber("decayIntervalSteps"),
      decayPercent: readNumber("decayPercent"),
      linkMemory: readNumber("linkMemory"),
      linkLearningRate: readNumber("linkLearningRate"),
      linkBonusMax: readNumber("linkBonusMax"),
      seed: readNumber("seed"),
      roundsPerSecond: readNumber("roundsPerSecond"),
      maxRounds: readNumber("maxRounds"),
      enforceTheoremAssumptions: readCheckbox("enforceTheoremAssumptions"),
    };

    this.currentConfig = normalizeConfig(config);
    return { ...this.currentConfig };
  }

  /**
   * @returns {any}
   */
  readResearchRequest() {
    const baseConfig = this.readConfig();

    const readNumber = (name, fallback) => {
      const input = this.container.querySelector(`[name='${name}']`);
      const value = Number(input?.value ?? fallback);
      return Number.isFinite(value) ? value : fallback;
    };

    const matrixText =
      this.container.querySelector("[name='researchMatrixText']")?.value ?? "";

    return {
      baseConfig,
      seedStart: readNumber("researchSeedStart", baseConfig.seed),
      seedCount: readNumber("researchSeedCount", 3),
      roundsPerCheck: readNumber(
        "researchRoundsPerCheck",
        Math.max(baseConfig.maxRounds, 350),
      ),
      optimizationIterations: readNumber("researchOptimizationIterations", 12),
      matrixText,
      nodeCountMin: readNumber(
        "researchNodeMin",
        Math.max(8, baseConfig.nodeCount - 12),
      ),
      nodeCountMax: readNumber("researchNodeMax", baseConfig.nodeCount + 12),
      nodeCountStep: readNumber("researchNodeStep", 8),
      linkRadiusMin: readNumber(
        "researchRadiusMin",
        Math.max(60, baseConfig.linkRadius - 30),
      ),
      linkRadiusMax: readNumber(
        "researchRadiusMax",
        baseConfig.linkRadius + 30,
      ),
      linkRadiusStep: readNumber("researchRadiusStep", 20),
    };
  }

  startResearchProgress() {
    this.clearResearchHideTimeout();
    this.researchProgress = {
      visible: true,
      running: true,
      completed: 0,
      total: 0,
      networkId: "-",
      stageId: "-",
      seed: "-",
      startedAt: Date.now(),
      statusText: "Preparing research batch...",
    };
    this.syncResearchProgressUi();
  }

  /**
   * @param {{completed:number,total:number,networkId?:string,stageId?:string,candidateId?:string,seed?:number}} info
   */
  updateResearchProgress(info) {
    this.researchProgress = {
      ...this.researchProgress,
      visible: true,
      running: true,
      completed: Number(info?.completed || 0),
      total: Number(info?.total || 0),
      networkId: info?.networkId || this.researchProgress.networkId || "-",
      stageId:
        info?.stageId ||
        info?.candidateId ||
        this.researchProgress.stageId ||
        "-",
      seed:
        info?.seed ??
        (this.researchProgress.seed !== undefined
          ? this.researchProgress.seed
          : "-"),
      statusText: "Batch execution in progress...",
    };
    this.syncResearchProgressUi();
  }

  /**
   * @param {boolean} failed
   * @param {string} message
   */
  finishResearchProgress(failed, message = "") {
    this.clearResearchHideTimeout();

    const statusText = failed
      ? `Research failed: ${message || "Unknown error"}`
      : "Research completed. HTML report generated (JSON/CSV buttons inside).";

    this.researchProgress = {
      ...this.researchProgress,
      visible: true,
      running: false,
      statusText,
    };
    this.syncResearchProgressUi();

    const timeoutMs = failed ? 5000 : 1800;
    this.researchHideTimeout = window.setTimeout(() => {
      this.researchProgress = {
        ...this.researchProgress,
        visible: false,
      };
      this.syncResearchProgressUi();
      this.researchHideTimeout = null;
    }, timeoutMs);
  }

  clearResearchHideTimeout() {
    if (this.researchHideTimeout !== null) {
      window.clearTimeout(this.researchHideTimeout);
      this.researchHideTimeout = null;
    }
  }

  syncResearchProgressUi() {
    const popup = this.container.querySelector(
      "[data-role='research-progress-window']",
    );
    if (!popup) {
      return;
    }

    popup.classList.toggle("is-active", Boolean(this.researchProgress.visible));

    const completed = Number(this.researchProgress.completed || 0);
    const total = Number(this.researchProgress.total || 0);
    const percent = total > 0 ? Math.round((completed / total) * 100) : 0;

    const percentNode = this.container.querySelector(
      "[data-role='research-progress-percent']",
    );
    if (percentNode) {
      percentNode.textContent = `${percent}%`;
    }

    const statusNode = this.container.querySelector(
      "[data-role='research-progress-status']",
    );
    if (statusNode) {
      statusNode.textContent = this.researchProgress.statusText || "-";
    }

    const runsNode = this.container.querySelector(
      "[data-role='research-progress-runs']",
    );
    if (runsNode) {
      runsNode.textContent = `${completed}/${total}`;
    }

    const targetNode = this.container.querySelector(
      "[data-role='research-progress-target']",
    );
    if (targetNode) {
      const networkId = this.researchProgress.networkId || "-";
      const stageId = this.researchProgress.stageId || "-";
      const seed = this.researchProgress.seed ?? "-";
      targetNode.textContent = `${networkId} | ${stageId} | seed ${seed}`;
    }

    const elapsedNode = this.container.querySelector(
      "[data-role='research-progress-elapsed']",
    );
    if (elapsedNode) {
      elapsedNode.textContent = formatElapsed(
        Date.now() - this.researchProgress.startedAt,
      );
    }

    const progressBar = this.container.querySelector(
      "[data-role='research-progress-bar']",
    );
    if (progressBar) {
      progressBar.value = percent;
      progressBar.max = 100;
    }

    const runButton = this.container.querySelector(
      "[data-action='research-batch']",
    );
    if (runButton) {
      runButton.disabled = Boolean(this.researchProgress.running);
      runButton.textContent = this.researchProgress.running
        ? "Research running..."
        : "Run Research Batch";
    }
  }

  /**
   * @param {boolean} running
   */
  setRunning(running) {
    this.isRunning = running;
    const button = this.container.querySelector("[data-action='run']");
    if (!button) {
      return;
    }

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
      <label for="${name}" title="${safeTooltip}">${label}: <span class="small-mono" data-range-value="${name}">${formatRangeValue(name, value)}</span></label>
      <input id="${name}" name="${name}" type="range" value="${value}" min="${min}" max="${max}" step="${step}" title="${safeTooltip}">
    </div>
  `;
}

function textAreaControl(name, label, value, rows, tooltip, placeholder = "") {
  const safeTooltip = escapeAttr(tooltip);
  const safePlaceholder = escapeAttr(placeholder);
  const safeValue = escapeText(value);

  return `
    <div class="control" title="${safeTooltip}">
      <label for="${name}" title="${safeTooltip}">${label}</label>
      <textarea id="${name}" name="${name}" rows="${rows}" placeholder="${safePlaceholder}" title="${safeTooltip}">${safeValue}</textarea>
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

function escapeText(value) {
  return String(value)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;");
}

function formatRangeValue(name, value) {
  if (
    name === "deliveryProbability" ||
    name === "chargeSpreadFactor" ||
    name === "decayPercent" ||
    name === "switchHysteresisRatio" ||
    name === "linkLearningRate"
  ) {
    return Number(value).toFixed(2);
  }
  if (name === "linkMemory") {
    return Number(value).toFixed(3);
  }
  return String(value);
}

function openAttr(value) {
  return value ? "open" : "";
}

function researchProgressWindow(progress) {
  const completed = Number(progress?.completed || 0);
  const total = Number(progress?.total || 0);
  const percent = total > 0 ? Math.round((completed / total) * 100) : 0;
  const status = escapeText(progress?.statusText || "-");
  const target = escapeText(
    `${progress?.networkId || "-"} | ${progress?.stageId || "-"} | seed ${progress?.seed ?? "-"}`,
  );
  const elapsed = formatElapsed(
    Date.now() - Number(progress?.startedAt || Date.now()),
  );

  return `
    <aside class="research-progress-window ${progress?.visible ? "is-active" : ""}" data-role="research-progress-window" title="Batch research progress">
      <div class="research-progress-header">
        <strong>Research Batch Progress</strong>
        <span class="small-mono" data-role="research-progress-percent">${percent}%</span>
      </div>
      <progress data-role="research-progress-bar" value="${percent}" max="100"></progress>
      <div class="research-progress-row"><span>Status</span><span class="small-mono" data-role="research-progress-status">${status}</span></div>
      <div class="research-progress-row"><span>Runs</span><span class="small-mono" data-role="research-progress-runs">${completed}/${total}</span></div>
      <div class="research-progress-row"><span>Current</span><span class="small-mono" data-role="research-progress-target">${target}</span></div>
      <div class="research-progress-row"><span>Elapsed</span><span class="small-mono" data-role="research-progress-elapsed">${elapsed}</span></div>
    </aside>
  `;
}

function createInitialResearchProgress() {
  return {
    visible: false,
    running: false,
    completed: 0,
    total: 0,
    networkId: "-",
    stageId: "-",
    seed: "-",
    startedAt: Date.now(),
    statusText: "Idle",
  };
}

function formatElapsed(ms) {
  const safeMs = Math.max(0, Number(ms || 0));
  const seconds = Math.floor(safeMs / 1000);
  const minutes = Math.floor(seconds / 60);
  const restSeconds = seconds % 60;
  return `${minutes}m ${String(restSeconds).padStart(2, "0")}s`;
}

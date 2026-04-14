/* Purpose: Render theorem status, assumption checks, and broadcast metrics in a readable panel. */

import {
  captureDetailsState,
  createCollapsibleDetails,
  resolveOpenState,
} from "./collapsibleGroups.js";
import { clear, el } from "./dom.js";

export class MetricsPanel {
  /**
   * @param {HTMLElement} container
   */
  constructor(container) {
    this.container = container;
    this.panelCollapsed = false;
    this.groupOpenState = {
      round: true,
      assumptions: true,
      theorem: true,
      broadcast: true,
    };
  }

  /**
   * @param {any} state
   */
  render(state) {
    const previousState = captureDetailsState(this.container);
    this.groupOpenState = { ...this.groupOpenState, ...previousState };

    clear(this.container);
    this.container.classList.toggle("panel-collapsed", this.panelCollapsed);

    this.container.appendChild(
      el(
        "div",
        {
          className: "panel-toolbar",
          title: "Collapse or expand the full results panel.",
        },
        [
          el(
            "button",
            {
              "data-action": "toggle-panel",
              title: "Collapse/expand results panel.",
            },
            [this.panelCollapsed ? "Expand results" : "Collapse results"],
          ),
        ],
      ),
    );

    if (this.panelCollapsed) {
      this.bindEvents(state);
      return;
    }

    const report = state.lastTheoremReport;
    const broadcast = state.lastBroadcastReport;
    const decay = state.lastDecay ?? {
      triggered: false,
      epoch: 0,
      percent: 0,
      factor: 1,
    };
    const oscillation = state.lastOscillationReport ?? {
      changedParents: 0,
      flappingNodes: 0,
      totalTracked: 0,
      maxFlips: 0,
    };
    const up = state.lastUp ?? {
      attempted: 0,
      reachedGateway: 0,
      hops: 0,
      updates: 0,
    };

    this.container.appendChild(
      createCollapsibleDetails({
        id: "round",
        title: "Round",
        tooltip: "Execution progress metrics.",
        open: resolveOpenState(this.groupOpenState, "round", true),
        contentNodes: [
          this.row(
            "iteration",
            String(state.round),
            "Current simulation round index.",
          ),
          this.row(
            "stable rounds",
            String(state.stableRounds),
            "Consecutive rounds without parent changes.",
          ),
          this.row(
            "eligible nodes",
            String(report.eligibleCount),
            "Nodes with q_total >= q_forward.",
          ),
          this.row(
            "spread updates",
            String(state.lastSpread?.updates ?? 0),
            "How many nodes increased q_total in the latest spread round.",
          ),
          this.row(
            "UP reached root",
            `${up.reachedGateway}/${up.attempted}`,
            "How many UP attempts reached gateway in the latest round.",
          ),
          this.row(
            "UP hops",
            String(up.hops),
            "Total number of UP forwarding hops in the latest round.",
          ),
          this.row(
            "DECAY epoch",
            String(decay.epoch ?? 0),
            "Current global decay epoch applied by scheduler.",
          ),
          this.row(
            "DECAY percent",
            `${(Number(decay.percent ?? 0) * 100).toFixed(1)}%`,
            "Configured charge attenuation ratio for each decay epoch.",
          ),
          this.row(
            "DECAY this round",
            decay.triggered ? "YES" : "NO",
            "Whether a DECAY attenuation step was applied in this round.",
          ),
          this.row(
            "parent changes",
            String(oscillation.changedParents),
            "How many nodes changed parent in the latest round.",
          ),
          this.row(
            "flapping nodes",
            `${oscillation.flappingNodes}/${oscillation.totalTracked}`,
            "Nodes with repeated parent oscillation inside the recent history window.",
          ),
        ],
      }),
    );

    this.container.appendChild(
      createCollapsibleDetails({
        id: "assumptions",
        title: "Assumptions",
        tooltip: "Foundational assumptions used by theorem proof.",
        open: resolveOpenState(this.groupOpenState, "assumptions", true),
        contentNodes: [
          this.rowBool(
            "A5 root is unique max",
            report.a5,
            "Gateway must have strictly highest charge among eligible nodes.",
          ),
          this.rowBool(
            "A6 local progress",
            report.a6,
            "Each eligible non-root node must have an eligible neighbor with higher charge.",
          ),
          this.rowBool(
            "A7 strict parent rule",
            report.a7,
            "Assigned parent must be an eligible higher-charge neighbor.",
          ),
          this.row(
            "A6 violations",
            listOrDash(report.violationsA6),
            "Node ids that violate local progress assumption.",
          ),
        ],
      }),
    );

    this.container.appendChild(
      createCollapsibleDetails({
        id: "theorem",
        title: "Theorem",
        tooltip: "Formal lemma checks and final theorem verdict.",
        open: resolveOpenState(this.groupOpenState, "theorem", true),
        contentNodes: [
          this.rowBool(
            "Lemma 4.1 strict increase",
            report.lemma41,
            "Every parent edge must strictly increase charge.",
          ),
          this.rowBool(
            "Lemma 4.2 acyclic",
            report.lemma42,
            "Parent graph on eligible nodes must be cycle-free.",
          ),
          this.rowBool(
            "Lemma 4.3 reach gateway",
            report.lemma43,
            "Every eligible node must reach gateway by parent pointers.",
          ),
          this.row(
            "Cycle witness",
            listOrDash(report.cycleWitness),
            "If non-empty, these nodes form a detected cycle trace.",
          ),
          this.row(
            "Unreachable",
            listOrDash(report.unreachable),
            "Eligible nodes that do not reach gateway by parent links.",
          ),
          this.rowBool(
            "Theorem status",
            report.theoremPass,
            "Overall theorem pass status from assumptions and lemmas.",
          ),
        ],
      }),
    );

    this.container.appendChild(
      createCollapsibleDetails({
        id: "broadcast",
        title: "Broadcast",
        tooltip: "DOWN tree-broadcast simulation diagnostics.",
        open: resolveOpenState(this.groupOpenState, "broadcast", true),
        contentNodes: [
          this.row(
            "order",
            listOrDash(broadcast.order),
            "Node visitation order during simulated DOWN broadcast.",
          ),
          this.row(
            "duplicates",
            String(broadcast.duplicates),
            "Count of duplicate receptions during simulation.",
          ),
          this.row(
            "reached nodes",
            `${broadcast.reachedCount ?? 0}/${state.nodes.size}`,
            "How many devices received the latest DOWN packet.",
          ),
          this.row(
            "coverage",
            formatCoverage(broadcast.coverage),
            "Share of graph that received latest DOWN packet.",
          ),
          this.row(
            "duplicates trend",
            formatDuplicatesTrend(state.broadcastHistory),
            "Recent duplicate counts per DOWN round.",
          ),
          this.row(
            "mode",
            String(broadcast.mode ?? "hybrid"),
            "Forwarding mode used in DOWN phase for latest round.",
          ),
          this.row(
            "strongest link",
            formatStrongestLink(state),
            "Edge with highest accumulated traffic-based reinforcement score.",
          ),
          this.row(
            "gateway edge quality",
            formatGatewayEdgeQuality(state),
            "Average effective quality of links adjacent to gateway.",
          ),
          this.rowBool(
            "loop detected",
            !broadcast.loopDetected,
            "PASS means no loop detected in broadcast traversal.",
          ),
        ],
      }),
    );

    this.bindEvents(state);
  }

  /**
   * @param {any} state
   */
  bindEvents(state) {
    const toggle = this.container.querySelector("[data-action='toggle-panel']");
    if (!toggle) {
      return;
    }

    toggle.addEventListener("click", () => {
      this.groupOpenState = {
        ...this.groupOpenState,
        ...captureDetailsState(this.container),
      };
      this.panelCollapsed = !this.panelCollapsed;
      this.render(state);
    });
  }

  row(key, value, tooltip) {
    return el("div", { className: "metric-row" }, [
      el("span", { className: "metric-key", title: tooltip }, [key]),
      el("span", { className: "small-mono", title: tooltip }, [value]),
    ]);
  }

  rowBool(key, pass, tooltip) {
    let text = "PENDING";
    let className = "metric-value pending";

    if (pass === true) {
      text = "PASS";
      className = "metric-value ok";
    } else if (pass === false) {
      text = "FAIL";
      className = "metric-value fail";
    }

    return el("div", { className: "metric-row" }, [
      el("span", { className: "metric-key", title: tooltip }, [key]),
      el(
        "span",
        {
          className,
          title: tooltip,
        },
        [text],
      ),
    ]);
  }
}

function listOrDash(items) {
  if (!items || items.length === 0) {
    return "-";
  }
  return items.join(", ");
}

function formatCoverage(value) {
  const numeric = Number(value ?? 0);
  if (!Number.isFinite(numeric)) {
    return "0%";
  }
  return `${(numeric * 100).toFixed(1)}%`;
}

function formatDuplicatesTrend(history) {
  if (!Array.isArray(history) || history.length === 0) {
    return "-";
  }

  const recent = history.slice(-6).map((item) => `${item.duplicates}`);
  return recent.join(" -> ");
}

function formatStrongestLink(state) {
  if (!state || !state.linkStats || !state.edges) {
    return "-";
  }

  let best = null;

  for (const edge of state.edges) {
    const key = edge.a < edge.b ? `${edge.a}:${edge.b}` : `${edge.b}:${edge.a}`;
    const stat = state.linkStats.get(key);
    if (!stat) {
      continue;
    }

    if (!best || stat.totalUsage > best.totalUsage) {
      best = {
        a: edge.a,
        b: edge.b,
        totalUsage: stat.totalUsage,
        effectiveQuality: stat.effectiveQuality,
      };
    }
  }

  if (!best) {
    return "-";
  }

  return `${best.a}-${best.b} q=${best.effectiveQuality.toFixed(2)} use=${best.totalUsage.toFixed(1)}`;
}

function formatGatewayEdgeQuality(state) {
  if (!state || !state.linkStats || !state.edges) {
    return "-";
  }

  const rootId = 0;
  const qualities = [];

  for (const edge of state.edges) {
    if (edge.a !== rootId && edge.b !== rootId) {
      continue;
    }

    const key = edge.a < edge.b ? `${edge.a}:${edge.b}` : `${edge.b}:${edge.a}`;
    const stat = state.linkStats.get(key);
    if (!stat) {
      continue;
    }

    qualities.push(stat.effectiveQuality);
  }

  if (qualities.length === 0) {
    return "-";
  }

  const avg =
    qualities.reduce((sum, value) => sum + value, 0) / qualities.length;
  return avg.toFixed(2);
}

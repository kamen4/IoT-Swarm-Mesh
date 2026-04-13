/* Purpose: Render theorem status, assumption checks, and broadcast metrics in a readable panel. */

import { clear, el } from "./dom.js";

export class MetricsPanel {
  /**
   * @param {HTMLElement} container
   */
  constructor(container) {
    this.container = container;
  }

  /**
   * @param {any} state
   */
  render(state) {
    clear(this.container);

    const report = state.lastTheoremReport;
    const broadcast = state.lastBroadcastReport;

    this.container.appendChild(
      el("div", { className: "group", title: "Execution progress metrics." }, [
        el("h3", { title: "Round progress and stability." }, ["Round"]),
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
      ]),
    );

    this.container.appendChild(
      el(
        "div",
        {
          className: "group",
          title: "Foundational assumptions used by theorem proof.",
        },
        [
          el("h3", { title: "Assumptions A5, A6, A7 status." }, [
            "Assumptions",
          ]),
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
      ),
    );

    this.container.appendChild(
      el(
        "div",
        {
          className: "group",
          title: "Formal lemma checks and final theorem verdict.",
        },
        [
          el("h3", { title: "Lemma checks for the induced parent tree." }, [
            "Theorem",
          ]),
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
      ),
    );

    this.container.appendChild(
      el(
        "div",
        {
          className: "group",
          title: "DOWN tree-broadcast simulation diagnostics.",
        },
        [
          el("h3", { title: "Observed broadcast behavior on current tree." }, [
            "Broadcast",
          ]),
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
          this.rowBool(
            "loop detected",
            !broadcast.loopDetected,
            "PASS means no loop detected in broadcast traversal.",
          ),
        ],
      ),
    );
  }

  row(key, value, tooltip) {
    return el("div", { className: "metric-row" }, [
      el("span", { className: "metric-key", title: tooltip }, [key]),
      el("span", { className: "small-mono", title: tooltip }, [value]),
    ]);
  }

  rowBool(key, pass, tooltip) {
    return el("div", { className: "metric-row" }, [
      el("span", { className: "metric-key", title: tooltip }, [key]),
      el(
        "span",
        {
          className: pass ? "metric-value ok" : "metric-value fail",
          title: tooltip,
        },
        [pass ? "PASS" : "FAIL"],
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

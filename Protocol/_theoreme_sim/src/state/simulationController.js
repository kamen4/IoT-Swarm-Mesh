/* Purpose: Execute simulation rounds, maintain convergence status, and coordinate UI rendering. */

import { simulateDownBroadcast } from "../broadcast/downBroadcastSimulation.js";
import { propagateNeighborChargesRound } from "../propagation/neighborChargePropagation.js";
import { rebuildTree } from "../routing/treeBuilder.js";
import { evaluateTheorem } from "../verification/theoremChecks.js";
import {
  createSimulationState,
  refreshChargeBounds,
  refreshEligibility,
} from "./simulationState.js";

export class SimulationController {
  /**
   * @param {import('../render/canvasRenderer.js').CanvasRenderer} renderer
   * @param {import('../ui/metricsPanel.js').MetricsPanel} metricsPanel
   */
  constructor(renderer, metricsPanel) {
    this.renderer = renderer;
    this.metricsPanel = metricsPanel;
    this.state = null;
    this.intervalId = null;
    this.onRunningChange = null;
  }

  /**
   * @param {(running:boolean) => void} callback
   */
  setRunningCallback(callback) {
    this.onRunningChange = callback;
  }

  /**
   * @param {any} config
   */
  generate(config) {
    this.pause();
    this.state = createSimulationState(config);
    this.evaluateAndRender();
  }

  /**
   * @param {any} config
   */
  reset(config) {
    this.generate(config);
  }

  step() {
    if (!this.state) {
      return;
    }

    if (this.state.round >= this.state.config.maxRounds) {
      this.pause();
      return;
    }

    this.state.round += 1;
    refreshEligibility(this.state.nodes, this.state.config.qForward);

    this.state.lastPropagation = propagateNeighborChargesRound(this.state);

    const rebuilt = rebuildTree(this.state);
    this.state.parentMap = rebuilt.parentMap;
    this.state.childrenMap = rebuilt.childrenMap;

    this.state.stableRounds =
      rebuilt.changedCount === 0 ? this.state.stableRounds + 1 : 0;

    refreshChargeBounds(this.state);
    this.evaluateAndRender();
  }

  toggleRun() {
    if (!this.state) {
      return;
    }

    if (this.intervalId !== null) {
      this.pause();
      return;
    }

    const ms = Math.max(
      20,
      Math.floor(1000 / this.state.config.roundsPerSecond),
    );
    this.intervalId = window.setInterval(() => {
      this.step();
    }, ms);

    if (this.onRunningChange) {
      this.onRunningChange(true);
    }
  }

  pause() {
    if (this.intervalId !== null) {
      window.clearInterval(this.intervalId);
      this.intervalId = null;
      if (this.onRunningChange) {
        this.onRunningChange(false);
      }
    }
  }

  simulateBroadcast() {
    if (!this.state) {
      return;
    }

    this.state.lastBroadcastReport = simulateDownBroadcast(this.state);
    this.metricsPanel.render(this.state);
  }

  rerender() {
    if (!this.state) {
      return;
    }
    this.renderer.render(this.state);
    this.metricsPanel.render(this.state);
  }

  evaluateAndRender() {
    this.state.lastTheoremReport = evaluateTheorem(this.state);
    this.renderer.render(this.state);
    this.metricsPanel.render(this.state);
  }
}

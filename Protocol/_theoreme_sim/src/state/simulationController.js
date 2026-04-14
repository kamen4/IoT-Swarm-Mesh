/* Purpose: Execute simulation rounds, maintain convergence status, and coordinate UI rendering. */

import { simulateDownBroadcast } from "../broadcast/downBroadcastSimulation.js";
import { normalizeConfig } from "../config/configNormalizer.js";
import { exportSimulationResults } from "../export/resultsExporter.js";
import { runBatchResearchStudy } from "../research/batchResearchRunner.js";
import { buildResearchReportHtml } from "../research/htmlReportBuilder.js";
import { buildReportData } from "../research/reportDataBuilder.js";
import { exportResearchArtifacts } from "../research/researchExporter.js";
import { evaluateTheorem } from "../verification/theoremChecks.js";
import { runNoUiSimulation as executeNoUiSimulation } from "./noUiSimulationRunner.js";
import { advanceSimulationRound } from "./simulationStep.js";
import { createSimulationState } from "./simulationState.js";

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
    this.state = createSimulationState(normalizeConfig(config));
    this.state.lastTheoremReport = evaluateTheorem(this.state);
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

    const result = advanceSimulationRound(this.state);
    if (!result.advanced) {
      this.pause();
      return;
    }

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

  /**
   * @param {Record<string, any>} patch
   */
  updateConfigLive(patch) {
    if (!this.state || !patch) {
      return;
    }

    Object.assign(
      this.state.config,
      normalizeConfig({ ...this.state.config, ...patch }),
    );
  }

  /**
   * @param {any} config
   * @param {number} maxSteps
   */
  runNoUiSimulation(config, maxSteps) {
    this.pause();

    const normalized = normalizeConfig(config);

    const { state, snapshots, stepsExecuted, finalSnapshot } =
      executeNoUiSimulation(normalized, maxSteps);

    this.state = state;
    this.evaluateAndRender();

    exportSimulationResults({
      finalSnapshot,
      snapshots,
      filePrefix: "no-ui-simulation",
    });

    window.alert(
      `No UI Simulation completed: ${stepsExecuted} steps. JSON and CSV exports were generated.`,
    );
  }

  /**
   * @param {{
   *  baseConfig:any,
   *  seedStart?:number,
   *  seedCount?:number,
   *  roundsPerCheck?:number,
   *  matrixText?:string,
   *  nodeCountMin?:number,
   *  nodeCountMax?:number,
   *  nodeCountStep?:number,
   *  linkRadiusMin?:number,
   *  linkRadiusMax?:number,
   *  linkRadiusStep?:number,
   *  optimizationIterations?:number,
   *  yieldEveryRuns?:number,
   *  onProgress?:(info:{completed:number,total:number,networkId?:string,stageId?:string,candidateId?:string,seed?:number}) => void,
   * }} request
   */
  async runResearchBatch(request) {
    this.pause();

    try {
      const safeBaseConfig = normalizeConfig(
        request?.baseConfig || this.state?.config || {},
      );

      const study = await runBatchResearchStudy({
        ...request,
        baseConfig: safeBaseConfig,
      });

      const reportData = buildReportData(study);
      const html = buildResearchReportHtml(reportData);

      exportResearchArtifacts({
        filePrefix: "research-batch-report",
        html,
      });

      window.alert(
        `Research batch completed: ${reportData.metadata.totalRuns} runs across ${reportData.metadata.networkCount} topologies. HTML report exported; JSON and CSV are available from buttons inside the report.`,
      );

      return reportData;
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error);
      console.error("Research batch failed", error);
      window.alert(`Research batch failed: ${message}`);
      throw error;
    }
  }

  rerender() {
    if (!this.state) {
      return;
    }
    this.renderer.render(this.state);
    this.metricsPanel.render(this.state);
  }

  evaluateAndRender() {
    if (!this.state) {
      return;
    }

    this.renderer.render(this.state);
    this.metricsPanel.render(this.state);
  }
}

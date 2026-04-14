/* Purpose: Bootstrap the theorem simulator app by wiring controller, renderer, and UI panels. */

import { DEFAULT_CONFIG } from "./core/constants.js";
import { CanvasRenderer } from "./render/canvasRenderer.js";
import { SimulationController } from "./state/simulationController.js";
import { ControlPanel } from "./ui/controlPanel.js";
import { InfoModal } from "./ui/infoModal.js";
import { MetricsPanel } from "./ui/metricsPanel.js";

const canvas = document.getElementById("sim-canvas");
const controlPanelRoot = document.getElementById("control-panel");
const metricsPanelRoot = document.getElementById("metrics-panel");
const theoremInfoButton = document.getElementById("theorem-info-button");
const theoremInfoModal = document.getElementById("theorem-info-modal");
const theoremInfoClose = document.getElementById("theorem-info-close");

const renderer = new CanvasRenderer(canvas);
const metricsPanel = new MetricsPanel(metricsPanelRoot);
const controller = new SimulationController(renderer, metricsPanel);

new InfoModal(theoremInfoButton, theoremInfoModal, theoremInfoClose);

const controls = new ControlPanel(
  controlPanelRoot,
  (config) => controller.generate(config),
  () => controller.step(),
  () => controller.toggleRun(),
  (config) => controller.reset(config),
  () => controller.simulateBroadcast(),
  (patch) => controller.updateConfigLive(patch),
  (config, maxSteps) => controller.runNoUiSimulation(config, maxSteps),
  (request) => controller.runResearchBatch(request),
);

controller.setRunningCallback((running) => {
  controls.setRunning(running);
});

controller.generate(DEFAULT_CONFIG);

window.addEventListener("resize", () => {
  controller.rerender();
});

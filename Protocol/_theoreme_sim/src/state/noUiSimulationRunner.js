/* Purpose: Run headless max-step simulation for batch export without per-step rendering overhead. */

import {
  buildFinalSnapshot,
  buildRoundSnapshot,
} from "../export/snapshotBuilder.js";
import { normalizeConfig } from "../config/configNormalizer.js";
import { createSimulationState } from "./simulationState.js";
import { advanceSimulationRound } from "./simulationStep.js";

/**
 * @param {any} config
 * @param {number} maxSteps
 * @returns {{state:any,snapshots:any[],stepsExecuted:number,finalSnapshot:any}}
 */
export function runNoUiSimulation(config, maxSteps) {
  const normalizedConfig = normalizeConfig(config);
  const state = createSimulationState(normalizedConfig);
  const snapshots = [];
  const limit = Math.max(
    1,
    Number(maxSteps || normalizedConfig.maxRounds || 1),
  );

  let stepsExecuted = 0;
  for (let i = 0; i < limit; i += 1) {
    const result = advanceSimulationRound(state);
    if (!result.advanced) {
      break;
    }
    stepsExecuted += 1;
    snapshots.push(buildRoundSnapshot(state));
  }

  const finalSnapshot = buildFinalSnapshot(state, snapshots, stepsExecuted);
  return { state, snapshots, stepsExecuted, finalSnapshot };
}

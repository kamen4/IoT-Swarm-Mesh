/* Purpose: Execute one full simulation round with shared logic for UI and No-UI runners. */

import { maybeRunDecayPhase } from "../phases/decayPhase.js";
import { runDownRoundPhase } from "../phases/downRoundPhase.js";
import { runUpRoundPhase } from "../phases/upRoundPhase.js";
import { finalizeLinkStrengthRound } from "../propagation/linkUsageTracker.js";
import { applyChargeSpreadRound } from "../propagation/chargeSpreadRound.js";
import { propagateNeighborChargesRound } from "../propagation/neighborChargePropagation.js";
import { rebuildTree } from "../routing/treeBuilder.js";
import { updateOscillationReport } from "../verification/oscillationDetector.js";
import { evaluateTheorem } from "../verification/theoremChecks.js";
import { refreshChargeBounds, refreshEligibility } from "./simulationState.js";

/**
 * @param {any} state
 * @returns {{advanced:boolean}}
 */
export function advanceSimulationRound(state) {
  if (!state) {
    return { advanced: false };
  }

  if (state.round >= state.config.maxRounds) {
    return { advanced: false };
  }

  state.round += 1;

  state.lastBroadcastReport = runDownRoundPhase(state);
  state.lastUp = runUpRoundPhase(state);
  state.lastPropagation = propagateNeighborChargesRound(state);
  state.lastSpread = applyChargeSpreadRound(state);

  finalizeLinkStrengthRound(state);

  refreshEligibility(state.nodes, state.config.qForward);

  let rebuilt = rebuildTree(state);
  state.parentMap = rebuilt.parentMap;
  state.childrenMap = rebuilt.childrenMap;
  let finalChangedCount = rebuilt.changedCount;

  state.lastDecay = maybeRunDecayPhase(state);
  if (state.lastDecay.triggered) {
    refreshEligibility(state.nodes, state.config.qForward);
    rebuilt = rebuildTree(state);
    state.parentMap = rebuilt.parentMap;
    state.childrenMap = rebuilt.childrenMap;
    finalChangedCount = rebuilt.changedCount;
  }

  state.stableRounds = finalChangedCount === 0 ? state.stableRounds + 1 : 0;

  refreshChargeBounds(state);
  state.lastOscillationReport = updateOscillationReport(state);
  state.lastTheoremReport = evaluateTheorem(state);

  if (!Array.isArray(state.broadcastHistory)) {
    state.broadcastHistory = [];
  }
  state.broadcastHistory.push({
    round: state.round,
    duplicates: state.lastBroadcastReport.duplicates,
    reachedCount: state.lastBroadcastReport.reachedCount,
    coverage: state.lastBroadcastReport.coverage,
  });

  if (state.broadcastHistory.length > 300) {
    state.broadcastHistory.splice(0, state.broadcastHistory.length - 300);
  }

  return { advanced: true };
}

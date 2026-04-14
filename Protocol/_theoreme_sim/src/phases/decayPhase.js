/* Purpose: Trigger periodic DECAY epochs and apply charge/estimate attenuation by configured schedule. */

import { applyGlobalDecay } from "../propagation/decayModel.js";
import { decayLinkStats } from "../propagation/linkUsageTracker.js";

/**
 * @param {number} value
 * @param {number} min
 * @param {number} max
 * @returns {number}
 */
function clamp(value, min, max) {
  return Math.min(max, Math.max(min, value));
}

/**
 * @param {any} state
 * @returns {{triggered:boolean,epoch:number,percent:number,factor:number}}
 */
export function maybeRunDecayPhase(state) {
  const interval = Math.max(
    0,
    Math.round(Number(state.config.decayIntervalSteps || 0)),
  );
  if (interval <= 0) {
    return {
      triggered: false,
      epoch: state.decayEpoch ?? 0,
      percent: 0,
      factor: 1,
    };
  }

  if (state.round <= 0 || state.round % interval !== 0) {
    return {
      triggered: false,
      epoch: state.decayEpoch ?? 0,
      percent: 0,
      factor: 1,
    };
  }

  const percent = clamp(Number(state.config.decayPercent || 0), 0, 0.8);
  const factor = 1 - percent;
  const epoch = (state.decayEpoch ?? 0) + 1;

  applyGlobalDecay(state.nodes, state.estimates, factor);
  decayLinkStats(state, factor);

  state.decayEpoch = epoch;

  if (!Array.isArray(state.decayHistory)) {
    state.decayHistory = [];
  }

  state.decayHistory.push({
    round: state.round,
    epoch,
    percent,
    factor,
  });

  if (state.decayHistory.length > 200) {
    state.decayHistory.splice(0, state.decayHistory.length - 200);
  }

  return {
    triggered: true,
    epoch,
    percent,
    factor,
  };
}

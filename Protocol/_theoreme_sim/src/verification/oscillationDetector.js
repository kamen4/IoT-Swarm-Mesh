/* Purpose: Detect parent flapping and report oscillation metrics over a rolling round window. */

import { THEOREM_ROOT_ID } from "../core/constants.js";

const HISTORY_WINDOW = 12;

/**
 * @param {any} state
 * @returns {{changedParents:number,flappingNodes:number,totalTracked:number,maxFlips:number}}
 */
export function updateOscillationReport(state) {
  if (!state.parentTrace) {
    state.parentTrace = new Map();
  }

  let changedParents = 0;
  let flappingNodes = 0;
  let maxFlips = 0;
  let totalTracked = 0;

  for (const [nodeId, parentId] of state.parentMap.entries()) {
    if (nodeId === THEOREM_ROOT_ID) {
      continue;
    }

    totalTracked += 1;

    const trace = state.parentTrace.get(nodeId) ?? {
      history: [],
      flips: 0,
      lastParent: null,
    };

    if (trace.lastParent !== null && trace.lastParent !== parentId) {
      trace.flips += 1;
      changedParents += 1;
    }

    trace.lastParent = parentId;
    trace.history.push(parentId);
    if (trace.history.length > HISTORY_WINDOW) {
      trace.history.shift();
    }

    const unique = new Set(trace.history.filter((item) => item !== null));
    const localFlips = countFlips(trace.history);
    if (unique.size >= 2 && localFlips >= 3) {
      flappingNodes += 1;
    }

    maxFlips = Math.max(maxFlips, trace.flips);
    state.parentTrace.set(nodeId, trace);
  }

  return {
    changedParents,
    flappingNodes,
    totalTracked,
    maxFlips,
  };
}

/**
 * @param {Array<number|null>} history
 * @returns {number}
 */
function countFlips(history) {
  let flips = 0;

  for (let i = 1; i < history.length; i += 1) {
    const prev = history[i - 1];
    const next = history[i];

    if (prev !== null && next !== null && prev !== next) {
      flips += 1;
    }
  }

  return flips;
}

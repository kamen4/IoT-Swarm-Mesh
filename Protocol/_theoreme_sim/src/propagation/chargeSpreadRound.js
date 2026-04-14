/* Purpose: Update node charges per round based on learned neighbor estimates instead of static initialization. */

import { THEOREM_ROOT_ID } from "../core/constants.js";

/**
 * @param {{
 *  nodes:Map<number,{id:number,qTotal:number,isGateway:boolean}>,
 *  estimates:Map<number,Map<number,number>>,
 *  config:{rootSourceCharge:number,chargeDropPerHop:number,chargeSpreadFactor:number}
 * }} state
 * @returns {{updates:number}}
 */
export function applyChargeSpreadRound(state) {
  const next = new Map();

  for (const node of state.nodes.values()) {
    if (node.id === THEOREM_ROOT_ID || node.isGateway) {
      next.set(node.id, Math.max(node.qTotal, state.config.rootSourceCharge));
      continue;
    }

    const estimateMap = state.estimates.get(node.id) ?? new Map();
    let bestEstimate = 0;

    for (const estimate of estimateMap.values()) {
      bestEstimate = Math.max(bestEstimate, estimate);
    }

    const target = Math.max(0, bestEstimate - state.config.chargeDropPerHop);
    const factor = Math.min(1, Math.max(0.01, state.config.chargeSpreadFactor));
    const blended = node.qTotal + (target - node.qTotal) * factor;

    // Keep q_total monotonic to preserve theorem-oriented parent potential behavior.
    const nextCharge = Math.max(node.qTotal, blended);
    next.set(node.id, nextCharge);
  }

  let updates = 0;
  for (const node of state.nodes.values()) {
    const value = next.get(node.id);
    if (value > node.qTotal) {
      updates += 1;
    }
    node.qTotal = value;
  }

  return { updates };
}

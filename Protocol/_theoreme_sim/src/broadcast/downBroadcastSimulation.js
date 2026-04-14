/* Purpose: Simulate DOWN tree-broadcast over eligible children and report duplicates/loops. */

import { THEOREM_ROOT_ID } from "../core/constants.js";

/**
 * @param {{nodes:Map<number,{eligible:boolean}>,childrenMap:Map<number,Set<number>>}} state
 * @returns {{order:number[],duplicates:number,loopDetected:boolean}}
 */
export function simulateDownBroadcast(state) {
  const received = new Set();
  const order = [];
  let duplicates = 0;

  const queue = [THEOREM_ROOT_ID];
  received.add(THEOREM_ROOT_ID);

  while (queue.length > 0) {
    const current = queue.shift();
    order.push(current);

    const children = state.childrenMap.get(current) ?? new Set();
    for (const childId of children.values()) {
      const child = state.nodes.get(childId);
      if (!child || !child.eligible) {
        continue;
      }

      if (received.has(childId)) {
        duplicates += 1;
        continue;
      }

      received.add(childId);
      queue.push(childId);
    }
  }

  return {
    order,
    duplicates,
    loopDetected: duplicates > 0,
    reachedCount: received.size,
    coverage:
      state.nodes.size > 0 ? received.size / Math.max(1, state.nodes.size) : 0,
    deliveries: order.length,
    propagationUpdates: 0,
    spreadUpdates: 0,
    mode: "tree-only",
  };
}

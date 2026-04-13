/* Purpose: Apply optional network-wide decay for charges and neighbor estimate tables. */

/**
 * @param {Map<number,{qTotal:number}>} nodes
 * @param {Map<number,Map<number,number>>} estimates
 * @param {number} factor
 */
export function applyGlobalDecay(nodes, estimates, factor) {
  const clamped = Math.min(Math.max(factor, 0.01), 1);

  for (const node of nodes.values()) {
    node.qTotal *= clamped;
  }

  for (const neighborMap of estimates.values()) {
    for (const [neighborId, value] of neighborMap.entries()) {
      neighborMap.set(neighborId, value * clamped);
    }
  }
}

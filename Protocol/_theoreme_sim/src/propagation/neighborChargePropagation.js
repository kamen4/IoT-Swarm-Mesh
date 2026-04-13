/* Purpose: Simulate asynchronous neighbor charge learning with max-based estimate updates. */

/**
 * @param {{
 *  edges:Array<{a:number,b:number}>,
 *  nodes:Map<number,{qTotal:number}>,
 *  estimates:Map<number,Map<number,number>>,
 *  config:{deliveryProbability:number},
 *  rng:() => number
 * }} state
 * @returns {{updates:number, deliveries:number}}
 */
export function propagateNeighborChargesRound(state) {
  let updates = 0;
  let deliveries = 0;

  const tryDeliver = (observerId, senderId) => {
    if (state.rng() > state.config.deliveryProbability) {
      return;
    }

    deliveries += 1;
    const estimateMap = state.estimates.get(observerId);
    const oldValue = estimateMap.get(senderId) ?? 0;
    const advertisedCharge = state.nodes.get(senderId).qTotal;
    const nextValue = Math.max(oldValue, advertisedCharge);

    if (nextValue > oldValue) {
      updates += 1;
      estimateMap.set(senderId, nextValue);
    }
  };

  for (const edge of state.edges) {
    tryDeliver(edge.a, edge.b);
    tryDeliver(edge.b, edge.a);
  }

  return { updates, deliveries };
}

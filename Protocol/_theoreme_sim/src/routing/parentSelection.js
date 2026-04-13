/* Purpose: Choose a strict higher-charge eligible parent using estimate-max and RSSI-like penalty. */

import { edgeKey } from "../core/graphUtils.js";

/**
 * @param {number} nodeId
 * @param {{
 *  nodes:Map<number,{id:number,qTotal:number,eligible:boolean,parent:number|null,isGateway:boolean}>,
 *  adjacency:Map<number,Set<number>>,
 *  estimates:Map<number,Map<number,number>>,
 *  edgeLookup:Map<string,{quality:number}>,
 *  config:{qForward:number,penaltyLambda:number,switchHysteresis:number}
 * }} state
 * @returns {number|null}
 */
export function chooseParent(nodeId, state) {
  const node = state.nodes.get(nodeId);
  if (node.isGateway || !node.eligible) {
    return null;
  }

  const neighbors = state.adjacency.get(nodeId) ?? new Set();
  const estimateMap = state.estimates.get(nodeId) ?? new Map();

  /** @type {Array<{id:number,penalty:number,score?:number,estimate:number}>} */
  const candidates = [];

  for (const neighborId of neighbors.values()) {
    const neighbor = state.nodes.get(neighborId);
    if (!neighbor || !neighbor.eligible) {
      continue;
    }

    // The theorem requires strict increase on real charges, not only estimates.
    if (neighbor.qTotal <= node.qTotal) {
      continue;
    }

    const estimate = estimateMap.get(neighborId) ?? 0;
    if (estimate <= node.qTotal || estimate < state.config.qForward) {
      continue;
    }

    const edge = state.edgeLookup.get(edgeKey(nodeId, neighborId));
    const quality = edge ? edge.quality : 0.1;
    const penalty = (1 - quality) * state.config.penaltyLambda;
    candidates.push({ id: neighborId, penalty, estimate });
  }

  if (candidates.length === 0) {
    return null;
  }

  const maxEstimate = Math.max(
    ...candidates.map((candidate) => candidate.estimate),
  );
  const tieWindow = Math.max(1, maxEstimate * 0.02);

  // Use penalty only among near-equal estimates as described in the theorem notes.
  const nearBest = candidates.filter(
    (candidate) => maxEstimate - candidate.estimate <= tieWindow,
  );

  for (const candidate of nearBest) {
    candidate.score = candidate.estimate - candidate.penalty;
  }

  nearBest.sort((a, b) => {
    if (b.score !== a.score) {
      return b.score - a.score;
    }
    if (b.estimate !== a.estimate) {
      return b.estimate - a.estimate;
    }
    return a.id - b.id;
  });

  const best = nearBest[0];
  const currentParent = node.parent;

  if (currentParent === null) {
    return best.id;
  }

  const current = nearBest.find((candidate) => candidate.id === currentParent);
  if (!current) {
    return best.id;
  }

  const keepCurrent =
    current.score + state.config.switchHysteresis >= best.score;
  return keepCurrent ? currentParent : best.id;
}

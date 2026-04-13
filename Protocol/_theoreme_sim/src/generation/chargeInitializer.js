/* Purpose: Initialize node charges so that gateway is unique maximum and charge gradient points toward it. */

import { THEOREM_ROOT_ID } from "../core/constants.js";
import { bfsDistances } from "../core/graphUtils.js";
import { randomInRange } from "../core/random.js";

/**
 * @param {Array<{id:number,x:number,y:number}>} nodes
 * @param {Map<number, Set<number>>} adjacency
 * @param {{qForward:number,enforceTheoremAssumptions:boolean}} config
 * @param {() => number} rng
 * @returns {{nodes:Array<{id:number,x:number,y:number,qTotal:number,isGateway:boolean,eligible:boolean,parent:null,children:number[]}>, distances:Map<number, number>}}
 */
export function initializeCharges(nodes, adjacency, config, rng) {
  const distances = bfsDistances(adjacency, THEOREM_ROOT_ID);
  let maxDistance = 0;

  for (const d of distances.values()) {
    if (Number.isFinite(d)) {
      maxDistance = Math.max(maxDistance, d);
    }
  }

  const initialized = nodes.map((node) => {
    const d = distances.get(node.id);
    const layerStrength = maxDistance - d + 1;
    const jitter = randomInRange(rng, -16, 16);

    // Charge decreases by graph-layer distance from gateway.
    let qTotal = 115 + layerStrength * 112 + jitter;

    if (node.id === THEOREM_ROOT_ID) {
      qTotal = 1620;
    }

    return {
      ...node,
      qTotal,
      isGateway: node.id === THEOREM_ROOT_ID,
      eligible: false,
      parent: null,
      children: [],
    };
  });

  // Optional strict repair: if a node has no higher-charge neighbor, lower its charge.
  if (config.enforceTheoremAssumptions) {
    const map = new Map(initialized.map((node) => [node.id, node]));

    for (const node of initialized) {
      if (node.isGateway) {
        continue;
      }

      const neighbors = adjacency.get(node.id);
      let hasHigher = false;
      for (const neighborId of neighbors) {
        if (map.get(neighborId).qTotal > node.qTotal) {
          hasHigher = true;
          break;
        }
      }

      if (!hasHigher) {
        const fallbackNeighborId = [...neighbors.values()].sort((a, b) => {
          return distances.get(a) - distances.get(b);
        })[0];

        const fallbackCharge = map.get(fallbackNeighborId).qTotal;
        node.qTotal = Math.min(node.qTotal, fallbackCharge - 20);
      }
    }

    const maxOther = Math.max(
      ...initialized
        .filter((node) => !node.isGateway)
        .map((node) => node.qTotal),
    );
    const rootNode = map.get(THEOREM_ROOT_ID);
    rootNode.qTotal = Math.max(rootNode.qTotal, maxOther + 80);
  }

  for (const node of initialized) {
    node.eligible = node.qTotal >= config.qForward;
  }

  return { nodes: initialized, distances };
}

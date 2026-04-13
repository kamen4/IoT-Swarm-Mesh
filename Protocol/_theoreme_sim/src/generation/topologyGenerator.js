/* Purpose: Build connected geometric mesh topologies suitable for theorem simulation rounds. */

import { LAYOUT } from "../core/constants.js";
import { randomInRange } from "../core/random.js";
import {
  buildAdjacency,
  connectedComponents,
  distance,
  edgeKey,
} from "../core/graphUtils.js";

/**
 * @param {number} nodeCount
 * @param {number} linkRadius
 * @param {() => number} rng
 * @returns {{nodes:Array<{id:number,x:number,y:number}>, edges:Array<{a:number,b:number,distance:number,quality:number}>, adjacency:Map<number, Set<number>>}}
 */
export function generateConnectedTopology(nodeCount, linkRadius, rng) {
  const nodes = [];

  for (let id = 0; id < nodeCount; id += 1) {
    nodes.push({
      id,
      x: randomInRange(rng, LAYOUT.margin, LAYOUT.width - LAYOUT.margin),
      y: randomInRange(rng, LAYOUT.margin, LAYOUT.height - LAYOUT.margin),
    });
  }

  /** @type {Array<{a:number,b:number,distance:number,quality:number}>} */
  const edges = [];
  const existing = new Set();

  const addEdge = (a, b) => {
    if (a === b) {
      return;
    }
    const key = edgeKey(a, b);
    if (existing.has(key)) {
      return;
    }

    const d = distance(nodes[a], nodes[b]);
    const quality = Math.max(0.08, 1 - d / (linkRadius * 1.35));
    edges.push({ a: Math.min(a, b), b: Math.max(a, b), distance: d, quality });
    existing.add(key);
  };

  for (let i = 0; i < nodeCount; i += 1) {
    for (let j = i + 1; j < nodeCount; j += 1) {
      if (distance(nodes[i], nodes[j]) <= linkRadius) {
        addEdge(i, j);
      }
    }
  }

  // Ensure each node has at least one neighbor by linking it to its nearest node.
  let adjacency = buildAdjacency(nodeCount, edges);
  for (let i = 0; i < nodeCount; i += 1) {
    if (adjacency.get(i).size > 0) {
      continue;
    }

    let nearest = -1;
    let nearestDistance = Number.POSITIVE_INFINITY;
    for (let j = 0; j < nodeCount; j += 1) {
      if (i === j) {
        continue;
      }
      const d = distance(nodes[i], nodes[j]);
      if (d < nearestDistance) {
        nearestDistance = d;
        nearest = j;
      }
    }
    addEdge(i, nearest);
  }

  adjacency = buildAdjacency(nodeCount, edges);

  // Merge disconnected components by nearest cross-component edges.
  let components = connectedComponents(adjacency);
  while (components.length > 1) {
    const primary = components[0];
    const secondary = components[1];
    let bestA = primary[0];
    let bestB = secondary[0];
    let bestDistance = Number.POSITIVE_INFINITY;

    for (const a of primary) {
      for (const b of secondary) {
        const d = distance(nodes[a], nodes[b]);
        if (d < bestDistance) {
          bestDistance = d;
          bestA = a;
          bestB = b;
        }
      }
    }

    addEdge(bestA, bestB);
    adjacency = buildAdjacency(nodeCount, edges);
    components = connectedComponents(adjacency);
  }

  return { nodes, edges, adjacency };
}

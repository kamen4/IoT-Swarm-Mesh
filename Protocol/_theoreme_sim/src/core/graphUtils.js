/* Purpose: Generic graph utilities for adjacency, BFS distances, edge lookup, and geometry. */

/**
 * @param {number} a
 * @param {number} b
 * @returns {string}
 */
export function edgeKey(a, b) {
  return a < b ? `${a}:${b}` : `${b}:${a}`;
}

/**
 * @param {{x:number,y:number}} u
 * @param {{x:number,y:number}} v
 * @returns {number}
 */
export function distance(u, v) {
  const dx = u.x - v.x;
  const dy = u.y - v.y;
  return Math.sqrt(dx * dx + dy * dy);
}

/**
 * @param {number} nodeCount
 * @param {{a:number,b:number}[]} edges
 * @returns {Map<number, Set<number>>}
 */
export function buildAdjacency(nodeCount, edges) {
  const adjacency = new Map();
  for (let i = 0; i < nodeCount; i += 1) {
    adjacency.set(i, new Set());
  }

  for (const edge of edges) {
    adjacency.get(edge.a).add(edge.b);
    adjacency.get(edge.b).add(edge.a);
  }

  return adjacency;
}

/**
 * @param {{a:number,b:number}[]} edges
 * @returns {Map<string, {a:number,b:number,distance:number,quality:number}>}
 */
export function buildEdgeLookup(edges) {
  const lookup = new Map();
  for (const edge of edges) {
    lookup.set(edgeKey(edge.a, edge.b), edge);
  }
  return lookup;
}

/**
 * @param {Map<number, Set<number>>} adjacency
 * @returns {number[][]}
 */
export function connectedComponents(adjacency) {
  const seen = new Set();
  const components = [];

  for (const id of adjacency.keys()) {
    if (seen.has(id)) {
      continue;
    }

    const queue = [id];
    const component = [];
    seen.add(id);

    while (queue.length > 0) {
      const current = queue.shift();
      component.push(current);

      for (const next of adjacency.get(current)) {
        if (!seen.has(next)) {
          seen.add(next);
          queue.push(next);
        }
      }
    }

    components.push(component);
  }

  return components;
}

/**
 * @param {Map<number, Set<number>>} adjacency
 * @param {number} rootId
 * @returns {Map<number, number>}
 */
export function bfsDistances(adjacency, rootId) {
  const dist = new Map();
  for (const id of adjacency.keys()) {
    dist.set(id, Number.POSITIVE_INFINITY);
  }

  dist.set(rootId, 0);
  const queue = [rootId];

  while (queue.length > 0) {
    const current = queue.shift();
    const currentDist = dist.get(current);

    for (const next of adjacency.get(current)) {
      if (dist.get(next) === Number.POSITIVE_INFINITY) {
        dist.set(next, currentDist + 1);
        queue.push(next);
      }
    }
  }

  return dist;
}

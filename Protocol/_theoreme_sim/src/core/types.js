/* Purpose: JSDoc contracts and constructors for simulation data structures. */

/**
 * @typedef {Object} SimNode
 * @property {number} id
 * @property {number} x
 * @property {number} y
 * @property {number} qTotal
 * @property {boolean} isGateway
 * @property {boolean} eligible
 * @property {number|null} parent
 * @property {number[]} children
 */

/**
 * @typedef {Object} SimEdge
 * @property {number} a
 * @property {number} b
 * @property {number} distance
 * @property {number} quality
 */

/**
 * @typedef {Object} BroadcastReport
 * @property {number[]} order
 * @property {number} duplicates
 * @property {boolean} loopDetected
 */

/**
 * @typedef {Object} TheoremReport
 * @property {boolean} assumptionsPass
 * @property {boolean} theoremPass
 * @property {boolean} a5
 * @property {boolean} a6
 * @property {boolean} a7
 * @property {boolean} lemma41
 * @property {boolean} lemma42
 * @property {boolean} lemma43
 * @property {number[]} violationsA6
 * @property {number[]} unreachable
 * @property {number[]} cycleWitness
 */

/**
 * @param {SimNode[]} nodes
 * @returns {Map<number, SimNode>}
 */
export function mapNodesById(nodes) {
  return new Map(nodes.map((node) => [node.id, node]));
}

/**
 * @param {Map<number, Set<number>>} adjacency
 * @returns {Map<number, Map<number, number>>}
 */
export function createEstimateMap(adjacency) {
  const outer = new Map();
  for (const [id, neighbors] of adjacency.entries()) {
    const inner = new Map();
    for (const neighborId of neighbors.values()) {
      inner.set(neighborId, 0);
    }
    outer.set(id, inner);
  }
  return outer;
}

/**
 * @returns {BroadcastReport}
 */
export function createEmptyBroadcastReport() {
  return {
    order: [],
    duplicates: 0,
    loopDetected: false,
  };
}

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
 * @property {number} reachedCount
 * @property {number} coverage
 * @property {number} deliveries
 * @property {number} propagationUpdates
 * @property {number} spreadUpdates
 * @property {string} mode
 */

/**
 * @typedef {Object} TheoremReport
 * @property {boolean|null} assumptionsPass
 * @property {boolean|null} theoremPass
 * @property {boolean|null} a5
 * @property {boolean|null} a6
 * @property {boolean|null} a7
 * @property {boolean|null} lemma41
 * @property {boolean|null} lemma42
 * @property {boolean|null} lemma43
 * @property {number[]} violationsA6
 * @property {number[]} unreachable
 * @property {number[]} cycleWitness
 * @property {"pending"|"pass"|"fail"} verificationState
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
    reachedCount: 0,
    coverage: 0,
    deliveries: 0,
    propagationUpdates: 0,
    spreadUpdates: 0,
    mode: "hybrid",
  };
}

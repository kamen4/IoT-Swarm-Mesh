/* Purpose: Build initial simulation state with topology, charges, estimate tables, and metadata. */

import { buildEdgeLookup } from "../core/graphUtils.js";
import { createSeededRng } from "../core/random.js";
import {
  createEmptyBroadcastReport,
  createEstimateMap,
  mapNodesById,
} from "../core/types.js";
import { initializeCharges } from "../generation/chargeInitializer.js";
import { generateConnectedTopology } from "../generation/topologyGenerator.js";

/**
 * @param {Map<number,{qTotal:number}>} nodes
 * @returns {{minCharge:number,maxCharge:number}}
 */
function chargeBoundsFromNodes(nodes) {
  let minCharge = Number.POSITIVE_INFINITY;
  let maxCharge = Number.NEGATIVE_INFINITY;

  for (const node of nodes.values()) {
    minCharge = Math.min(minCharge, node.qTotal);
    maxCharge = Math.max(maxCharge, node.qTotal);
  }

  if (!Number.isFinite(minCharge) || !Number.isFinite(maxCharge)) {
    return { minCharge: 0, maxCharge: 1 };
  }

  return { minCharge, maxCharge };
}

/**
 * @param {Map<number,{qTotal:number}>} nodes
 * @param {number} qForward
 */
export function refreshEligibility(nodes, qForward) {
  for (const node of nodes.values()) {
    node.eligible = node.qTotal >= qForward;
  }
}

/**
 * @param {any} config
 * @returns {any}
 */
export function createSimulationState(config) {
  const rng = createSeededRng(config.seed);
  const topology = generateConnectedTopology(
    config.nodeCount,
    config.linkRadius,
    rng,
  );
  const charged = initializeCharges(
    topology.nodes,
    topology.adjacency,
    config,
    rng,
  );
  const nodes = mapNodesById(charged.nodes);
  const estimates = createEstimateMap(topology.adjacency);

  const parentMap = new Map();
  const childrenMap = new Map();
  for (const node of nodes.values()) {
    parentMap.set(node.id, null);
    childrenMap.set(node.id, new Set());
  }

  return {
    config,
    rng,
    nodes,
    edges: topology.edges,
    adjacency: topology.adjacency,
    edgeLookup: buildEdgeLookup(topology.edges),
    estimates,
    distances: charged.distances,
    parentMap,
    childrenMap,
    round: 0,
    stableRounds: 0,
    lastPropagation: { updates: 0, deliveries: 0 },
    lastTheoremReport: {
      assumptionsPass: false,
      theoremPass: false,
      eligibleCount: 0,
      a5: false,
      a6: false,
      a7: false,
      lemma41: false,
      lemma42: false,
      lemma43: false,
      violationsA6: [],
      unreachable: [],
      cycleWitness: [],
    },
    lastBroadcastReport: createEmptyBroadcastReport(),
    chargeBounds: chargeBoundsFromNodes(nodes),
  };
}

/**
 * @param {any} state
 */
export function refreshChargeBounds(state) {
  state.chargeBounds = chargeBoundsFromNodes(state.nodes);
}

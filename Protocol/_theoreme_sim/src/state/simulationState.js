/* Purpose: Build initial simulation state with topology, charges, estimate tables, and metadata. */

import { buildEdgeLookup } from "../core/graphUtils.js";
import { createSeededRng } from "../core/random.js";
import {
  createEmptyBroadcastReport,
  createEstimateMap,
  mapNodesById,
} from "../core/types.js";
import { normalizeConfig } from "../config/configNormalizer.js";
import { initializeCharges } from "../generation/chargeInitializer.js";
import { generateConnectedTopology } from "../generation/topologyGenerator.js";
import { createLinkStats } from "../propagation/linkUsageTracker.js";

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
  const normalizedConfig = normalizeConfig(config);
  const rng = createSeededRng(normalizedConfig.seed);
  const topology = generateConnectedTopology(
    normalizedConfig.nodeCount,
    normalizedConfig.linkRadius,
    rng,
  );
  const charged = initializeCharges(
    topology.nodes,
    topology.adjacency,
    normalizedConfig,
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
    config: normalizedConfig,
    rng,
    nodes,
    edges: topology.edges,
    adjacency: topology.adjacency,
    edgeLookup: buildEdgeLookup(topology.edges),
    linkStats: createLinkStats(topology.edges),
    estimates,
    distances: charged.distances,
    parentMap,
    childrenMap,
    round: 0,
    stableRounds: 0,
    decayEpoch: 0,
    decayHistory: [],
    lastDecay: {
      triggered: false,
      epoch: 0,
      percent: 0,
      factor: 1,
    },
    parentTrace: new Map(),
    lastOscillationReport: {
      changedParents: 0,
      flappingNodes: 0,
      totalTracked: 0,
      maxFlips: 0,
    },
    lastPropagation: { updates: 0, deliveries: 0 },
    lastUp: { attempted: 0, reachedGateway: 0, hops: 0, updates: 0 },
    lastSpread: { updates: 0 },
    lastTheoremReport: {
      assumptionsPass: null,
      theoremPass: null,
      eligibleCount: 0,
      a5: null,
      a6: null,
      a7: null,
      lemma41: null,
      lemma42: null,
      lemma43: null,
      violationsA6: [],
      unreachable: [],
      cycleWitness: [],
      verificationState: "pending",
    },
    lastBroadcastReport: createEmptyBroadcastReport(),
    broadcastHistory: [],
    chargeBounds: chargeBoundsFromNodes(nodes),
  };
}

/**
 * @param {any} state
 */
export function refreshChargeBounds(state) {
  state.chargeBounds = chargeBoundsFromNodes(state.nodes);
}

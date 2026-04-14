/* Purpose: Simulate one DOWN phase tick from root to mesh and track duplicates/coverage. */

import { THEOREM_ROOT_ID } from "../core/constants.js";
import { recordLinkUsage } from "../propagation/linkUsageTracker.js";

/**
 * @param {number} value
 * @returns {number}
 */
function clampSpreadFactor(value) {
  return Math.min(1, Math.max(0.01, Number(value || 0)));
}

/**
 * @param {number} observerId
 * @param {number} senderId
 * @param {number} advertisedCharge
 * @param {any} state
 * @returns {boolean}
 */
function updateNeighborEstimate(observerId, senderId, advertisedCharge, state) {
  const estimateMap = state.estimates.get(observerId);
  if (!estimateMap) {
    return false;
  }

  const previous = estimateMap.get(senderId) ?? 0;
  const next = Math.max(previous, advertisedCharge);

  if (next > previous) {
    estimateMap.set(senderId, next);
    return true;
  }

  return false;
}

/**
 * @param {number} nodeId
 * @param {number|null} fromId
 * @param {any} state
 * @returns {number[]}
 */
function selectForwardTargets(nodeId, fromId, state) {
  const neighbors = state.adjacency.get(nodeId) ?? new Set();

  if (nodeId === THEOREM_ROOT_ID) {
    return [...neighbors.values()];
  }

  const node = state.nodes.get(nodeId);
  if (!node) {
    return [];
  }

  const targets = new Set();

  if (node.eligible) {
    const children = state.childrenMap.get(nodeId) ?? new Set();
    for (const childId of children.values()) {
      const child = state.nodes.get(childId);
      if (child && child.eligible) {
        targets.add(childId);
      }
    }

    // Keep coverage for still-cold nodes while the tree is warming up.
    for (const neighborId of neighbors.values()) {
      if (neighborId === fromId) {
        continue;
      }

      const neighbor = state.nodes.get(neighborId);
      if (neighbor && !neighbor.eligible) {
        targets.add(neighborId);
      }
    }

    return [...targets.values()];
  }

  for (const neighborId of neighbors.values()) {
    if (neighborId !== fromId) {
      targets.add(neighborId);
    }
  }

  return [...targets.values()];
}

/**
 * @param {any} state
 * @returns {{
 *  order:number[],
 *  duplicates:number,
 *  loopDetected:boolean,
 *  reachedCount:number,
 *  coverage:number,
 *  deliveries:number,
 *  propagationUpdates:number,
 *  spreadUpdates:number,
 *  mode:string
 * }}
 */
export function runDownRoundPhase(state) {
  const root = state.nodes.get(THEOREM_ROOT_ID);
  if (!root) {
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

  root.qTotal = Math.max(root.qTotal, state.config.rootSourceCharge);

  const spreadFactor = clampSpreadFactor(state.config.chargeSpreadFactor);
  const transmissions = [];
  const received = new Set([THEOREM_ROOT_ID]);
  const order = [THEOREM_ROOT_ID];

  let duplicates = 0;
  let deliveries = 1;
  let propagationUpdates = 0;
  let spreadUpdates = 0;

  for (const targetId of selectForwardTargets(THEOREM_ROOT_ID, null, state)) {
    transmissions.push({
      fromId: THEOREM_ROOT_ID,
      toId: targetId,
      advertisedCharge: root.qTotal,
    });
  }

  while (transmissions.length > 0) {
    const tx = transmissions.shift();
    const receiver = state.nodes.get(tx.toId);
    if (!receiver) {
      continue;
    }

    recordLinkUsage(state, tx.fromId, tx.toId, 1);

    if (received.has(tx.toId)) {
      duplicates += 1;
      continue;
    }

    received.add(tx.toId);
    order.push(tx.toId);
    deliveries += 1;

    if (
      updateNeighborEstimate(tx.toId, tx.fromId, tx.advertisedCharge, state)
    ) {
      propagationUpdates += 1;
    }

    const before = receiver.qTotal;
    const targetCharge = Math.max(
      0,
      tx.advertisedCharge - state.config.chargeDropPerHop,
    );
    const blended = before + (targetCharge - before) * spreadFactor;
    const nextCharge = Math.max(before, blended);

    if (nextCharge > before) {
      spreadUpdates += 1;
    }

    receiver.qTotal = nextCharge;

    for (const targetId of selectForwardTargets(tx.toId, tx.fromId, state)) {
      transmissions.push({
        fromId: tx.toId,
        toId: targetId,
        advertisedCharge: receiver.qTotal,
      });
    }
  }

  const reachedCount = received.size;
  const coverage =
    state.nodes.size > 0 ? reachedCount / Math.max(1, state.nodes.size) : 0;

  return {
    order,
    duplicates,
    loopDetected: duplicates > 0,
    reachedCount,
    coverage,
    deliveries,
    propagationUpdates,
    spreadUpdates,
    mode: "hybrid",
  };
}

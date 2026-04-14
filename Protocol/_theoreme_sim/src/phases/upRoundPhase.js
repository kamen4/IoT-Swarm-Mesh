/* Purpose: Simulate one UP phase sweep where each device attempts to send toward gateway. */

import { THEOREM_ROOT_ID } from "../core/constants.js";
import {
  getLinkDeliveryProbability,
  recordLinkUsage,
} from "../propagation/linkUsageTracker.js";

/**
 * @param {number} observerId
 * @param {number} senderId
 * @param {any} state
 * @returns {boolean}
 */
function updateNeighborEstimate(observerId, senderId, state) {
  const observer = state.nodes.get(observerId);
  const sender = state.nodes.get(senderId);
  const estimateMap = state.estimates.get(observerId);

  if (!observer || !sender || !estimateMap) {
    return false;
  }

  const previous = estimateMap.get(senderId) ?? 0;
  const next = Math.max(previous, sender.qTotal);
  if (next > previous) {
    estimateMap.set(senderId, next);
    return true;
  }

  return false;
}

/**
 * @param {number} nodeId
 * @param {Set<number>} visited
 * @param {any} state
 * @returns {number|null}
 */
function chooseUpNextHop(nodeId, visited, state) {
  const node = state.nodes.get(nodeId);
  if (!node) {
    return null;
  }

  const preferredParent = state.parentMap.get(nodeId) ?? node.parent ?? null;
  if (preferredParent !== null && !visited.has(preferredParent)) {
    const parent = state.nodes.get(preferredParent);
    if (parent && parent.qTotal > node.qTotal) {
      return preferredParent;
    }
  }

  const neighbors = state.adjacency.get(nodeId) ?? new Set();
  let bestId = null;
  let bestCharge = Number.NEGATIVE_INFINITY;

  for (const neighborId of neighbors.values()) {
    if (visited.has(neighborId)) {
      continue;
    }

    const neighbor = state.nodes.get(neighborId);
    if (!neighbor) {
      continue;
    }

    if (neighbor.qTotal <= node.qTotal) {
      continue;
    }

    if (
      neighbor.qTotal > bestCharge ||
      (neighbor.qTotal === bestCharge &&
        (bestId === null || neighborId < bestId))
    ) {
      bestCharge = neighbor.qTotal;
      bestId = neighborId;
    }
  }

  return bestId;
}

/**
 * @param {any} state
 * @returns {{attempted:number,reachedGateway:number,hops:number,updates:number}}
 */
export function runUpRoundPhase(state) {
  const hopGain = Math.max(
    1,
    Math.round(Math.max(1, state.config.chargeDropPerHop) * 0.03),
  );

  let attempted = 0;
  let reachedGateway = 0;
  let hops = 0;
  let updates = 0;

  const nodeIds = [...state.nodes.keys()].sort((a, b) => a - b);

  for (const sourceId of nodeIds) {
    if (sourceId === THEOREM_ROOT_ID) {
      continue;
    }

    attempted += 1;

    let currentId = sourceId;
    const visited = new Set([sourceId]);
    const safetyLimit = Math.max(1, state.nodes.size);

    for (let step = 0; step < safetyLimit; step += 1) {
      const nextId = chooseUpNextHop(currentId, visited, state);
      if (nextId === null) {
        break;
      }

      const sender = state.nodes.get(currentId);
      const receiver = state.nodes.get(nextId);
      if (!sender || !receiver) {
        break;
      }

      const probability = getLinkDeliveryProbability(state, currentId, nextId);
      if (state.rng() > probability) {
        break;
      }

      recordLinkUsage(state, currentId, nextId, 1);

      const senderBefore = sender.qTotal;
      const receiverBefore = receiver.qTotal;

      sender.qTotal += hopGain;
      receiver.qTotal += hopGain;

      if (sender.qTotal > senderBefore || receiver.qTotal > receiverBefore) {
        updates += 1;
      }

      if (updateNeighborEstimate(nextId, currentId, state)) {
        updates += 1;
      }

      hops += 1;
      currentId = nextId;

      if (currentId === THEOREM_ROOT_ID) {
        reachedGateway += 1;
        break;
      }

      if (visited.has(currentId)) {
        break;
      }
      visited.add(currentId);
    }
  }

  return {
    attempted,
    reachedGateway,
    hops,
    updates,
  };
}

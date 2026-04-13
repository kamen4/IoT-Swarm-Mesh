/* Purpose: Verify theorem assumptions A5, A6, A7 on the current simulation state. */

import { THEOREM_ROOT_ID } from "../core/constants.js";

/**
 * @param {{nodes:Map<number,{id:number,qTotal:number,eligible:boolean,parent:number|null,isGateway:boolean}>,adjacency:Map<number,Set<number>>}} state
 * @returns {number[]}
 */
export function getEligibleNodeIds(state) {
  return [...state.nodes.values()]
    .filter((node) => node.eligible)
    .map((node) => node.id)
    .sort((a, b) => a - b);
}

/**
 * @param {any} state
 * @returns {{pass:boolean,violations:number[]}}
 */
export function checkA5UniqueGatewayMaximum(state) {
  const eligible = getEligibleNodeIds(state);
  const root = state.nodes.get(THEOREM_ROOT_ID);

  if (!root || !root.eligible) {
    return { pass: false, violations: eligible };
  }

  const violations = eligible.filter((id) => {
    if (id === THEOREM_ROOT_ID) {
      return false;
    }
    return !(root.qTotal > state.nodes.get(id).qTotal);
  });

  return { pass: violations.length === 0, violations };
}

/**
 * @param {any} state
 * @returns {{pass:boolean,violations:number[]}}
 */
export function checkA6LocalProgress(state) {
  const violations = [];

  for (const node of state.nodes.values()) {
    if (!node.eligible || node.isGateway) {
      continue;
    }

    const neighbors = state.adjacency.get(node.id) ?? new Set();
    let hasHigherEligibleNeighbor = false;

    for (const neighborId of neighbors.values()) {
      const neighbor = state.nodes.get(neighborId);
      if (neighbor.eligible && neighbor.qTotal > node.qTotal) {
        hasHigherEligibleNeighbor = true;
        break;
      }
    }

    if (!hasHigherEligibleNeighbor) {
      violations.push(node.id);
    }
  }

  return { pass: violations.length === 0, violations };
}

/**
 * @param {any} state
 * @returns {{pass:boolean,violations:number[]}}
 */
export function checkA7ParentRule(state) {
  const violations = [];

  for (const node of state.nodes.values()) {
    if (!node.eligible || node.isGateway) {
      continue;
    }

    if (node.parent === null) {
      violations.push(node.id);
      continue;
    }

    const neighbors = state.adjacency.get(node.id) ?? new Set();
    if (!neighbors.has(node.parent)) {
      violations.push(node.id);
      continue;
    }

    const parent = state.nodes.get(node.parent);
    if (!parent || !parent.eligible || parent.qTotal <= node.qTotal) {
      violations.push(node.id);
    }
  }

  return { pass: violations.length === 0, violations };
}

/**
 * @param {any} state
 * @returns {{a5:{pass:boolean,violations:number[]},a6:{pass:boolean,violations:number[]},a7:{pass:boolean,violations:number[]}}}
 */
export function checkAssumptions(state) {
  const a5 = checkA5UniqueGatewayMaximum(state);
  const a6 = checkA6LocalProgress(state);
  const a7 = checkA7ParentRule(state);
  return { a5, a6, a7 };
}

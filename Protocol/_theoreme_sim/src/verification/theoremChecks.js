/* Purpose: Evaluate theorem lemmas and final theorem status for the eligible induced parent graph. */

import { THEOREM_ROOT_ID } from "../core/constants.js";
import { checkAssumptions, getEligibleNodeIds } from "./assumptionChecks.js";

/**
 * @param {any} state
 * @returns {{pass:boolean,violations:number[]}}
 */
function checkLemma41StrictIncrease(state) {
  const violations = [];

  for (const node of state.nodes.values()) {
    if (!node.eligible || node.isGateway || node.parent === null) {
      continue;
    }

    const parent = state.nodes.get(node.parent);
    if (!parent || !(parent.qTotal > node.qTotal)) {
      violations.push(node.id);
    }
  }

  return { pass: violations.length === 0, violations };
}

/**
 * @param {any} state
 * @returns {{pass:boolean,cycleWitness:number[]}}
 */
function checkLemma42Acyclic(state) {
  const eligibleSet = new Set(getEligibleNodeIds(state));
  const visitedGlobal = new Set();

  for (const startId of eligibleSet.values()) {
    if (visitedGlobal.has(startId)) {
      continue;
    }

    const localIndex = new Map();
    const path = [];
    let current = startId;

    while (current !== null && eligibleSet.has(current)) {
      if (localIndex.has(current)) {
        const cycleStart = localIndex.get(current);
        return { pass: false, cycleWitness: path.slice(cycleStart) };
      }
      if (visitedGlobal.has(current)) {
        break;
      }

      localIndex.set(current, path.length);
      path.push(current);
      visitedGlobal.add(current);
      current = state.parentMap.get(current) ?? null;
    }
  }

  return { pass: true, cycleWitness: [] };
}

/**
 * @param {any} state
 * @returns {{pass:boolean,unreachable:number[]}}
 */
function checkLemma43Reachability(state) {
  const eligible = getEligibleNodeIds(state);
  const unreachable = [];

  for (const id of eligible) {
    if (id === THEOREM_ROOT_ID) {
      continue;
    }

    const seen = new Set();
    let current = id;
    let reached = false;

    while (current !== null) {
      if (seen.has(current)) {
        reached = false;
        break;
      }
      seen.add(current);

      if (current === THEOREM_ROOT_ID) {
        reached = true;
        break;
      }

      current = state.parentMap.get(current) ?? null;
    }

    if (!reached) {
      unreachable.push(id);
    }
  }

  return { pass: unreachable.length === 0, unreachable };
}

/**
 * @param {any} state
 * @returns {{
 * assumptionsPass:boolean,
 * theoremPass:boolean,
 * eligibleCount:number,
 * a5:boolean,
 * a6:boolean,
 * a7:boolean,
 * lemma41:boolean,
 * lemma42:boolean,
 * lemma43:boolean,
 * violationsA6:number[],
 * unreachable:number[],
 * cycleWitness:number[]
 * }}
 */
export function evaluateTheorem(state) {
  const assumptions = checkAssumptions(state);
  const lemma41 = checkLemma41StrictIncrease(state);
  const lemma42 = checkLemma42Acyclic(state);
  const lemma43 = checkLemma43Reachability(state);

  const eligibleCount = getEligibleNodeIds(state).length;
  const eligibleNonRoot = Math.max(0, eligibleCount - 1);
  const assignedParents = [...state.parentMap.entries()].filter(
    ([childId, parentId]) =>
      childId !== THEOREM_ROOT_ID &&
      parentId !== null &&
      state.nodes.get(childId).eligible,
  ).length;

  const spanningCondition = assignedParents >= eligibleNonRoot;

  const assumptionsPass =
    assumptions.a5.pass && assumptions.a6.pass && assumptions.a7.pass;
  const theoremPass =
    assumptionsPass &&
    lemma41.pass &&
    lemma42.pass &&
    lemma43.pass &&
    spanningCondition;

  return {
    assumptionsPass,
    theoremPass,
    eligibleCount,
    a5: assumptions.a5.pass,
    a6: assumptions.a6.pass,
    a7: assumptions.a7.pass,
    lemma41: lemma41.pass,
    lemma42: lemma42.pass,
    lemma43: lemma43.pass,
    violationsA6: assumptions.a6.violations,
    unreachable: lemma43.unreachable,
    cycleWitness: lemma42.cycleWitness,
  };
}

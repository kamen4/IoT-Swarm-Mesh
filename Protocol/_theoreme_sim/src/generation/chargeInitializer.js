/* Purpose: Initialize charges in a near-empty state so charge growth happens during simulation rounds. */

import { THEOREM_ROOT_ID } from "../core/constants.js";
import { bfsDistances } from "../core/graphUtils.js";

/**
 * @param {Array<{id:number,x:number,y:number}>} nodes
 * @param {Map<number, Set<number>>} adjacency
 * @param {{qForward:number,rootSourceCharge:number}} config
 * @param {() => number} _rng
 * @returns {{nodes:Array<{id:number,x:number,y:number,qTotal:number,isGateway:boolean,eligible:boolean,parent:null,children:number[]}>, distances:Map<number, number>}}
 */
export function initializeCharges(nodes, adjacency, config, _rng) {
  const distances = bfsDistances(adjacency, THEOREM_ROOT_ID);

  const initialized = nodes.map((node) => {
    const qTotal = node.id === THEOREM_ROOT_ID ? config.rootSourceCharge : 0;

    return {
      ...node,
      qTotal,
      isGateway: node.id === THEOREM_ROOT_ID,
      eligible: false,
      parent: null,
      children: [],
    };
  });

  for (const node of initialized) {
    node.eligible = node.qTotal >= config.qForward;
  }

  return { nodes: initialized, distances };
}

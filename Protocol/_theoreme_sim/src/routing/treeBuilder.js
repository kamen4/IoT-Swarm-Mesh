/* Purpose: Rebuild parent pointers and derived children map from current local estimates. */

import { chooseParent } from "./parentSelection.js";

/**
 * @param {{
 *  nodes:Map<number,{id:number,isGateway:boolean,eligible:boolean,parent:number|null,children:number[]}>,
 *  parentMap:Map<number,number|null>
 * }} state
 * @returns {{changedCount:number,parentMap:Map<number,number|null>,childrenMap:Map<number,Set<number>>}}
 */
export function rebuildTree(state) {
  const nextParentMap = new Map();

  for (const node of state.nodes.values()) {
    if (node.isGateway || !node.eligible) {
      nextParentMap.set(node.id, null);
      continue;
    }

    nextParentMap.set(node.id, chooseParent(node.id, state));
  }

  let changedCount = 0;
  for (const node of state.nodes.values()) {
    const prev = state.parentMap.get(node.id) ?? null;
    const next = nextParentMap.get(node.id) ?? null;
    if (prev !== next) {
      changedCount += 1;
    }
    node.parent = next;
  }

  const childrenMap = new Map();
  for (const node of state.nodes.values()) {
    childrenMap.set(node.id, new Set());
  }

  for (const [childId, parentId] of nextParentMap.entries()) {
    if (parentId !== null && childrenMap.has(parentId)) {
      childrenMap.get(parentId).add(childId);
    }
  }

  for (const node of state.nodes.values()) {
    node.children = [...(childrenMap.get(node.id) ?? new Set())];
  }

  return { changedCount, parentMap: nextParentMap, childrenMap };
}

/* Purpose: Compute edge charge weights, normalized styles, and labels for graph links. */

import { normalize, edgeStyleFromNormalizedWeight } from "./colorScale.js";

/**
 * @param {{a:number,b:number}} edge
 * @param {Map<number,{qTotal:number}>} nodes
 * @param {Map<number,Map<number,number>>} estimates
 * @returns {number}
 */
export function computeEdgeWeight(edge, nodes, estimates) {
  const qAB = estimates.get(edge.a)?.get(edge.b) ?? nodes.get(edge.b).qTotal;
  const qBA = estimates.get(edge.b)?.get(edge.a) ?? nodes.get(edge.a).qTotal;
  return (qAB + qBA) / 2;
}

/**
 * @param {number} childId
 * @param {number} parentId
 * @param {Map<number,{qTotal:number}>} nodes
 * @returns {number}
 */
export function computeTreeDelta(childId, parentId, nodes) {
  const child = nodes.get(childId);
  const parent = nodes.get(parentId);
  return Math.max(0, parent.qTotal - child.qTotal);
}

/**
 * @param {number} value
 * @param {number} minCharge
 * @param {number} maxCharge
 * @param {boolean} isTreeEdge
 * @returns {{lineWidth:number,stroke:string,label:string,alpha:number}}
 */
export function styleForWeight(value, minCharge, maxCharge, isTreeEdge) {
  const normalized = normalize(value, minCharge, maxCharge);
  return edgeStyleFromNormalizedWeight(normalized, isTreeEdge);
}

/**
 * @param {number} value
 * @returns {string}
 */
export function formatWeight(value) {
  return `w=${value.toFixed(0)}`;
}

/**
 * @param {number} value
 * @returns {string}
 */
export function formatDelta(value) {
  return `dq=${value.toFixed(0)}`;
}

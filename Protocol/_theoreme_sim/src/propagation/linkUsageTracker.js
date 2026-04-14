/* Purpose: Track edge usage over rounds and convert traffic concentration into stronger effective links. */

import { edgeKey } from "../core/graphUtils.js";

/**
 * @param {number} value
 * @param {number} min
 * @param {number} max
 * @returns {number}
 */
function clamp(value, min, max) {
  return Math.min(max, Math.max(min, value));
}

/**
 * @param {any[]} edges
 * @returns {Map<string,{baseQuality:number,effectiveQuality:number,usageScore:number,roundUsage:number,totalUsage:number}>}
 */
export function createLinkStats(edges) {
  const map = new Map();

  for (const edge of edges) {
    const key = edgeKey(edge.a, edge.b);
    map.set(key, {
      baseQuality: clamp(Number(edge.quality || 0.1), 0.05, 1),
      effectiveQuality: clamp(Number(edge.quality || 0.1), 0.05, 1),
      usageScore: 0,
      roundUsage: 0,
      totalUsage: 0,
    });
  }

  return map;
}

/**
 * @param {any} state
 * @param {number} a
 * @param {number} b
 * @param {number} amount
 */
export function recordLinkUsage(state, a, b, amount = 1) {
  const key = edgeKey(a, b);
  const stat = state.linkStats?.get(key);
  if (!stat) {
    return;
  }

  const increment = Math.max(0, Number(amount || 0));
  stat.roundUsage += increment;
  stat.totalUsage += increment;
}

/**
 * @param {any} state
 * @param {number} a
 * @param {number} b
 * @returns {number}
 */
export function getEffectiveLinkQuality(state, a, b) {
  const key = edgeKey(a, b);
  const stat = state.linkStats?.get(key);

  if (stat) {
    return clamp(stat.effectiveQuality, 0.05, 1);
  }

  const edge = state.edgeLookup?.get(key);
  return clamp(Number(edge?.quality ?? 0.1), 0.05, 1);
}

/**
 * @param {any} state
 * @param {number} a
 * @param {number} b
 * @returns {number}
 */
export function getLinkDeliveryProbability(state, a, b) {
  const quality = getEffectiveLinkQuality(state, a, b);
  const base = Number(state.config.deliveryProbability || 0.5);

  // High-quality links become more reliable while low-quality links still have baseline chance.
  const probability = 0.04 + base * (0.2 + 0.8 * quality);
  return clamp(probability, 0.02, 1);
}

/**
 * @param {any} state
 */
export function finalizeLinkStrengthRound(state) {
  if (!state.linkStats) {
    return;
  }

  const memory = clamp(Number(state.config.linkMemory || 0.9), 0.6, 0.999);
  const learning = clamp(Number(state.config.linkLearningRate || 0.2), 0.01, 2);

  for (const stat of state.linkStats.values()) {
    stat.usageScore = stat.usageScore * memory + stat.roundUsage;

    const boost = 1 - Math.exp(-stat.usageScore * 0.035 * learning);
    const targetQuality = clamp(
      stat.baseQuality + (1 - stat.baseQuality) * boost,
      0.05,
      1,
    );

    stat.effectiveQuality =
      stat.effectiveQuality * (1 - learning * 0.12) +
      targetQuality * (learning * 0.12);
    stat.effectiveQuality = clamp(stat.effectiveQuality, 0.05, 1);
    stat.roundUsage = 0;
  }
}

/**
 * @param {any} state
 * @param {number} factor
 */
export function decayLinkStats(state, factor) {
  if (!state.linkStats) {
    return;
  }

  const clampedFactor = clamp(Number(factor || 1), 0, 1);
  for (const stat of state.linkStats.values()) {
    stat.usageScore *= clampedFactor;
    stat.effectiveQuality = clamp(
      stat.baseQuality +
        (stat.effectiveQuality - stat.baseQuality) * clampedFactor,
      0.05,
      1,
    );
  }
}

/**
 * @param {any} state
 * @param {number} parentId
 * @param {number} childId
 * @returns {number}
 */
export function computeLinkStabilityBonus(state, parentId, childId) {
  const key = edgeKey(parentId, childId);
  const stat = state.linkStats?.get(key);
  if (!stat) {
    return 0;
  }

  const bonusMax = Math.max(0, Number(state.config.linkBonusMax || 0));
  if (bonusMax <= 0) {
    return 0;
  }

  const normalized = 1 - Math.exp(-stat.usageScore * 0.02);
  return bonusMax * normalized;
}

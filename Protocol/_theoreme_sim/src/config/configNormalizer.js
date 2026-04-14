/* Purpose: Normalize and clamp simulation config to stable ranges and cross-parameter constraints. */

import { DEFAULT_CONFIG } from "../core/constants.js";

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
 * @param {number} value
 * @param {number} min
 * @param {number} max
 * @returns {number}
 */
function toInt(value, min, max) {
  const numeric = Number(value);
  if (!Number.isFinite(numeric)) {
    return min;
  }
  return Math.round(clamp(numeric, min, max));
}

/**
 * @param {number} value
 * @param {number} min
 * @param {number} max
 * @param {number} precision
 * @returns {number}
 */
function toFloat(value, min, max, precision = 2) {
  const numeric = Number(value);
  if (!Number.isFinite(numeric)) {
    return min;
  }

  const clamped = clamp(numeric, min, max);
  const factor = 10 ** precision;
  return Math.round(clamped * factor) / factor;
}

/**
 * @param {any} config
 * @returns {any}
 */
export function normalizeConfig(config) {
  const source = { ...DEFAULT_CONFIG, ...(config || {}) };

  const normalized = {
    nodeCount: toInt(source.nodeCount, 8, 220),
    linkRadius: toInt(source.linkRadius, 40, 420),
    qForward: toInt(source.qForward, 20, 1800),
    deliveryProbability: toFloat(source.deliveryProbability, 0.05, 1, 2),
    penaltyLambda: toInt(source.penaltyLambda, 0, 250),
    switchHysteresis: toInt(source.switchHysteresis, 0, 260),
    switchHysteresisRatio: toFloat(source.switchHysteresisRatio, 0, 0.4, 2),
    rootSourceCharge: toInt(source.rootSourceCharge, 250, 3000),
    chargeDropPerHop: toInt(source.chargeDropPerHop, 5, 420),
    chargeSpreadFactor: toFloat(source.chargeSpreadFactor, 0.02, 1, 2),
    seed: toInt(source.seed, 1, 999999),
    roundsPerSecond: toInt(source.roundsPerSecond, 1, 60),
    maxRounds: toInt(source.maxRounds, 20, 10000),
    enforceTheoremAssumptions: Boolean(source.enforceTheoremAssumptions),
    decayIntervalSteps: toInt(source.decayIntervalSteps, 0, 2000),
    decayPercent: toFloat(source.decayPercent, 0, 0.8, 2),
    linkMemory: toFloat(source.linkMemory, 0.6, 0.999, 3),
    linkLearningRate: toFloat(source.linkLearningRate, 0.01, 2, 2),
    linkBonusMax: toInt(source.linkBonusMax, 0, 240),
  };

  if (normalized.maxRounds < normalized.roundsPerSecond * 3) {
    normalized.maxRounds = normalized.roundsPerSecond * 3;
  }

  if (normalized.qForward >= normalized.rootSourceCharge) {
    normalized.qForward = Math.max(
      20,
      Math.floor(normalized.rootSourceCharge * 0.72),
    );
  }

  if (normalized.chargeDropPerHop >= normalized.rootSourceCharge) {
    normalized.chargeDropPerHop = Math.max(
      5,
      Math.floor(normalized.rootSourceCharge * 0.28),
    );
  }

  return normalized;
}

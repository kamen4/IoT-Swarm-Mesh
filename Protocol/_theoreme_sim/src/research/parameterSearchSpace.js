/* Purpose: Define adaptive parameter space helpers for gradient-like research optimization. */

import { normalizeConfig } from "../config/configNormalizer.js";
import { PARAMETER_RANGES } from "../config/parameterRanges.js";

const OPTIMIZATION_KEYS = [
  "qForward",
  "deliveryProbability",
  "rootSourceCharge",
  "penaltyLambda",
  "switchHysteresis",
  "switchHysteresisRatio",
  "chargeDropPerHop",
  "chargeSpreadFactor",
  "decayIntervalSteps",
  "decayPercent",
  "linkMemory",
  "linkLearningRate",
  "linkBonusMax",
];

export const OPTIMIZATION_PARAMETERS = OPTIMIZATION_KEYS.map((key) => {
  const range = PARAMETER_RANGES[key];
  return {
    key,
    label: key,
    min: Number(range.min),
    max: Number(range.max),
    type: range.type,
    precision: Number(range.precision ?? 0),
  };
});

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
 * @returns {number}
 */
function clampUnit(value) {
  return clamp(Number(value), 0, 1);
}

/**
 * @param {number} value
 * @param {number} precision
 * @returns {number}
 */
function roundTo(value, precision) {
  const factor = 10 ** precision;
  return Math.round(value * factor) / factor;
}

/**
 * @param {number} value
 * @param {{min:number,max:number}} param
 * @returns {number}
 */
function toUnit(value, param) {
  const span = Math.max(1e-9, param.max - param.min);
  return clampUnit((Number(value) - param.min) / span);
}

/**
 * @param {number} unit
 * @param {{min:number,max:number,type:string,precision:number}} param
 * @returns {number}
 */
function fromUnit(unit, param) {
  const span = Math.max(1e-9, param.max - param.min);
  const raw = param.min + clampUnit(unit) * span;

  if (param.type === "int") {
    return Math.round(raw);
  }
  return roundTo(raw, param.precision);
}

/**
 * @param {number[]} vector
 * @returns {number[]}
 */
export function clampVector(vector) {
  return (vector || []).map((value) => clampUnit(value));
}

/**
 * @param {any} config
 * @returns {number[]}
 */
export function encodeOptimizationVector(config) {
  const normalized = normalizeConfig(config || {});
  return OPTIMIZATION_PARAMETERS.map((param) =>
    toUnit(normalized[param.key], param),
  );
}

/**
 * @param {number[]} vector
 * @param {any} baseConfig
 * @returns {any}
 */
export function decodeOptimizationVector(vector, baseConfig) {
  const next = { ...(baseConfig || {}) };
  const unitVector = clampVector(vector);

  for (let i = 0; i < OPTIMIZATION_PARAMETERS.length; i += 1) {
    const param = OPTIMIZATION_PARAMETERS[i];
    next[param.key] = fromUnit(unitVector[i], param);
  }

  return normalizeConfig(next);
}

/**
 * @param {() => number} rng
 * @param {number} [activeDimensions]
 * @returns {number[]}
 */
export function randomDirection(rng, activeDimensions = 3) {
  const dimensionCount = OPTIMIZATION_PARAMETERS.length;
  const active = Math.max(
    1,
    Math.min(dimensionCount, Math.round(Number(activeDimensions) || 1)),
  );

  const direction = Array.from({ length: dimensionCount }, () => 0);
  const selected = new Set();

  while (selected.size < active) {
    selected.add(Math.floor(rng() * dimensionCount));
  }

  for (const index of selected) {
    direction[index] = rng() >= 0.5 ? 1 : -1;
  }

  return direction;
}

/**
 * @param {any} config
 * @returns {Record<string,number>}
 */
export function summarizeOptimizationParameters(config) {
  const normalized = normalizeConfig(config || {});
  const summary = {};

  for (const param of OPTIMIZATION_PARAMETERS) {
    summary[param.key] = Number(normalized[param.key]);
  }

  return summary;
}

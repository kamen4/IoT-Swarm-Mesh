/* Purpose: Generate a fully random simulation config using declared parameter ranges. */

import { normalizeConfig } from "./configNormalizer.js";
import { PARAMETER_RANGES } from "./parameterRanges.js";

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
 * @param {{type:string,min?:number,max?:number,precision?:number}} range
 * @param {() => number} rng
 * @returns {any}
 */
function randomValue(range, rng) {
  if (range.type === "boolean") {
    return rng() >= 0.5;
  }

  const min = range.min ?? 0;
  const max = range.max ?? 1;

  if (range.type === "int") {
    return Math.floor(min + rng() * (max - min + 1));
  }

  if (range.type === "float") {
    const raw = min + rng() * (max - min);
    return roundTo(raw, range.precision ?? 2);
  }

  return min;
}

/**
 * @param {Record<string, any>} baseConfig
 * @param {() => number} rng
 * @returns {Record<string, any>}
 */
export function randomizeAllConfig(baseConfig, rng = Math.random) {
  const next = { ...baseConfig };

  for (const [key, range] of Object.entries(PARAMETER_RANGES)) {
    next[key] = randomValue(range, rng);
  }

  return normalizeConfig(next);
}

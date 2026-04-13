/* Purpose: Map charge values to readable node and edge colors for theorem-focused visualization. */

/**
 * @param {number} value
 * @returns {number}
 */
function clamp01(value) {
  return Math.min(1, Math.max(0, value));
}

/**
 * @param {number} value
 * @param {number} min
 * @param {number} max
 * @returns {number}
 */
export function normalize(value, min, max) {
  if (max <= min) {
    return 0;
  }
  return clamp01((value - min) / (max - min));
}

/**
 * @param {number} charge
 * @param {number} minCharge
 * @param {number} maxCharge
 * @param {boolean} isGateway
 * @param {boolean} eligible
 * @returns {string}
 */
export function chargeToNodeColor(
  charge,
  minCharge,
  maxCharge,
  isGateway,
  eligible,
) {
  if (isGateway) {
    return "hsl(20, 84%, 50%)";
  }

  const t = normalize(charge, minCharge, maxCharge);
  const hue = 190 - t * 160;
  const sat = 66 + t * 14;
  const light = eligible ? 56 - t * 12 : 76;

  return `hsl(${hue.toFixed(0)}, ${sat.toFixed(0)}%, ${light.toFixed(0)}%)`;
}

/**
 * @param {number} normalizedWeight
 * @param {boolean} isTreeEdge
 * @returns {{lineWidth:number,stroke:string,label:string,alpha:number}}
 */
export function edgeStyleFromNormalizedWeight(normalizedWeight, isTreeEdge) {
  const n = clamp01(normalizedWeight);

  if (isTreeEdge) {
    const width = 2.2 + n * 4.8;
    const light = 48 + n * 26;
    const alpha = 0.64 + n * 0.3;
    return {
      lineWidth: width,
      stroke: `hsla(27, 82%, ${light.toFixed(0)}%, ${alpha.toFixed(2)})`,
      label: "rgba(111, 58, 12, 0.9)",
      alpha,
    };
  }

  const width = 0.9 + n * 3.2;
  const light = 32 + n * 38;
  const alpha = 0.24 + n * 0.42;
  return {
    lineWidth: width,
    stroke: `hsla(191, 76%, ${light.toFixed(0)}%, ${alpha.toFixed(2)})`,
    label: "rgba(15, 75, 73, 0.72)",
    alpha,
  };
}

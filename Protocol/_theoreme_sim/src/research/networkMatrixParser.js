/* Purpose: Parse research topology matrix settings into deterministic {nodeCount, linkRadius} combinations. */

/**
 * @param {number} value
 * @param {number} min
 * @param {number} max
 * @returns {number}
 */
function clampInt(value, min, max) {
  const n = Number(value);
  if (!Number.isFinite(n)) {
    return min;
  }
  return Math.round(Math.min(max, Math.max(min, n)));
}

/**
 * @param {number} min
 * @param {number} max
 * @param {number} step
 * @returns {number[]}
 */
function buildRange(min, max, step) {
  const safeMin = Math.min(min, max);
  const safeMax = Math.max(min, max);
  const safeStep = Math.max(1, step);

  const values = [];
  for (let value = safeMin; value <= safeMax; value += safeStep) {
    values.push(value);
  }

  if (values.length === 0 || values[values.length - 1] !== safeMax) {
    values.push(safeMax);
  }

  return values;
}

/**
 * @param {string} matrixText
 * @returns {Array<{nodeCount:number,linkRadius:number}>}
 */
function parseMatrixText(matrixText) {
  if (!matrixText || !matrixText.trim()) {
    return [];
  }

  const tokens = matrixText
    .split(/[\n,;]+/)
    .map((token) => token.trim())
    .filter(Boolean);

  const pairs = [];

  for (const token of tokens) {
    const match = token.match(/^(\d+)\s*[xX:]\s*(\d+)$/);
    if (!match) {
      continue;
    }

    pairs.push({
      nodeCount: clampInt(match[1], 8, 320),
      linkRadius: clampInt(match[2], 40, 600),
    });
  }

  return pairs;
}

/**
 * @param {{
 *  matrixText?:string,
 *  nodeCountMin:number,
 *  nodeCountMax:number,
 *  nodeCountStep:number,
 *  linkRadiusMin:number,
 *  linkRadiusMax:number,
 *  linkRadiusStep:number,
 * }} input
 * @returns {Array<{id:string,nodeCount:number,linkRadius:number,label:string}>}
 */
export function buildTopologyMatrix(input) {
  const fromText = parseMatrixText(input.matrixText || "");

  const pairs =
    fromText.length > 0
      ? fromText
      : (() => {
          const nodes = buildRange(
            clampInt(input.nodeCountMin, 8, 320),
            clampInt(input.nodeCountMax, 8, 320),
            clampInt(input.nodeCountStep, 1, 200),
          );

          const radii = buildRange(
            clampInt(input.linkRadiusMin, 40, 600),
            clampInt(input.linkRadiusMax, 40, 600),
            clampInt(input.linkRadiusStep, 1, 200),
          );

          const generated = [];
          for (const nodeCount of nodes) {
            for (const linkRadius of radii) {
              generated.push({ nodeCount, linkRadius });
            }
          }
          return generated;
        })();

  const dedup = new Map();

  for (const pair of pairs) {
    const key = `${pair.nodeCount}x${pair.linkRadius}`;
    if (!dedup.has(key)) {
      dedup.set(key, {
        id: key,
        nodeCount: pair.nodeCount,
        linkRadius: pair.linkRadius,
        label: `N=${pair.nodeCount}, R=${pair.linkRadius}`,
      });
    }
  }

  return [...dedup.values()];
}

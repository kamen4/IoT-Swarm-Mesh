/* Purpose: Deterministic pseudo-random generator to make simulations reproducible by seed. */

/**
 * @param {number} seed
 * @returns {() => number}
 */
export function createSeededRng(seed) {
  let x = (seed | 0) ^ 0x9e3779b9;

  return function next() {
    x ^= x << 13;
    x ^= x >>> 17;
    x ^= x << 5;
    const unsigned = x >>> 0;
    return unsigned / 4294967296;
  };
}

/**
 * @param {() => number} rng
 * @param {number} min
 * @param {number} max
 * @returns {number}
 */
export function randomInRange(rng, min, max) {
  return min + (max - min) * rng();
}

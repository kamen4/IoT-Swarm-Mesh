/* Purpose: Compare optimization candidates and adapt search step size on plateau/improvement. */

/**
 * @param {number} score
 * @param {number} stableRatio
 * @param {number} tailEligibleRatio
 * @param {number} flappingAvg
 * @param {number} scoreStdDev
 * @returns {number}
 */
function objectiveScore(
  score,
  stableRatio,
  tailEligibleRatio,
  flappingAvg,
  scoreStdDev,
) {
  const scoreComponent = Number(score || 0);
  const stableComponent = Number(stableRatio || 0) * 26;
  const tailHealthComponent = Number(tailEligibleRatio || 0) * 12;
  const flappingPenalty = Math.max(0, Number(flappingAvg || 0) - 0.6) * 2.4;
  const variancePenalty = Number(scoreStdDev || 0) * 0.85;

  return (
    scoreComponent +
    stableComponent +
    tailHealthComponent -
    flappingPenalty -
    variancePenalty
  );
}

/**
 * @param {{
 *  mode:string,
 *  vector:number[],
 *  score:number,
 *  stableRatio:number,
 *  tailEligibleRatio?:number,
 *  flappingAvg?:number,
 *  scoreStdDev?:number,
 * }} input
 * @returns {{
 *  mode:string,
 *  vector:number[],
 *  score:number,
 *  stableRatio:number,
 *  tailEligibleRatio:number,
 *  flappingAvg:number,
 *  scoreStdDev:number,
 *  objective:number,
 * }}
 */
export function createOptimizationCandidate(input) {
  const score = Number(input?.score || 0);
  const stableRatio = Number(input?.stableRatio || 0);
  const tailEligibleRatio = Number(input?.tailEligibleRatio || 0);
  const flappingAvg = Number(input?.flappingAvg || 0);
  const scoreStdDev = Number(input?.scoreStdDev || 0);

  return {
    mode: input?.mode || "hold",
    vector: input?.vector || [],
    score,
    stableRatio,
    tailEligibleRatio,
    flappingAvg,
    scoreStdDev,
    objective: objectiveScore(
      score,
      stableRatio,
      tailEligibleRatio,
      flappingAvg,
      scoreStdDev,
    ),
  };
}

/**
 * @param {{objective:number,stableRatio:number,score:number}} left
 * @param {{objective:number,stableRatio:number,score:number}} right
 * @returns {number}
 */
export function optimizationPreference(left, right) {
  const objectiveDelta =
    Number(left?.objective || 0) - Number(right?.objective || 0);
  if (Math.abs(objectiveDelta) > 1e-9) {
    return objectiveDelta;
  }

  const ratioDelta =
    Number(left?.stableRatio || 0) - Number(right?.stableRatio || 0);
  if (Math.abs(ratioDelta) > 1e-9) {
    return ratioDelta;
  }

  return Number(left?.score || 0) - Number(right?.score || 0);
}

/**
 * @param {{objective:number,stableRatio:number,score:number}} left
 * @param {{objective:number,stableRatio:number,score:number}} right
 * @returns {number}
 */
export function sortByOptimizationPreferenceDesc(left, right) {
  return optimizationPreference(right, left);
}

/**
 * @param {{objective:number}} winner
 * @param {{objective:number}} current
 * @param {number} stepSize
 * @returns {boolean}
 */
export function hasMeaningfulOptimizationGain(winner, current, stepSize) {
  const baseGate = Math.max(0.02, Number(stepSize || 0) * 0.08);
  const unstableRelax = Number(current?.stableRatio || 0) < 0.15 ? 0.008 : 0;
  const gate = Math.max(0.012, baseGate - unstableRelax);

  return (
    Number(winner?.objective || 0) > Number(current?.objective || 0) + gate
  );
}

/**
 * @param {number} currentStep
 * @param {boolean} improved
 * @param {number} objectiveDelta
 * @param {number} minStep
 * @param {number} stableRatio
 * @param {number} plateauStreak
 * @returns {number}
 */
export function nextOptimizationStep(
  currentStep,
  improved,
  objectiveDelta,
  minStep,
  stableRatio,
  plateauStreak,
) {
  const step = Number(currentStep || 0);
  const floor = Number(minStep || 0);
  const streak = Number(plateauStreak || 0);

  if (improved) {
    if (objectiveDelta > 3.2) {
      return Math.min(0.72, Math.max(floor, step * 1.05));
    }

    const shrink = objectiveDelta > 1.25 ? 0.92 : 0.86;
    return Math.max(floor, step * shrink);
  }

  if (streak >= 4) {
    const reheated = step * (1.18 + Math.min(0.36, (streak - 3) * 0.06));
    return Math.min(0.68, Math.max(floor, reheated));
  }

  const fallbackShrink = Number(stableRatio || 0) >= 0.34 ? 0.66 : 0.76;
  return Math.max(floor, step * fallbackShrink);
}

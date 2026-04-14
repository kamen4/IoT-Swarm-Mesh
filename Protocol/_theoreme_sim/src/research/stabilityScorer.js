/* Purpose: Convert long-run simulation snapshots into comparable stability score and verdict. */

/**
 * @param {number[]} values
 * @returns {number}
 */
function average(values) {
  if (!values || values.length === 0) {
    return 0;
  }
  return values.reduce((sum, value) => sum + value, 0) / values.length;
}

/**
 * @param {number[]} values
 * @returns {{early:number,late:number,delta:number}}
 */
function trendDelta(values) {
  if (!values || values.length === 0) {
    return { early: 0, late: 0, delta: 0 };
  }

  const chunk = Math.max(1, Math.floor(values.length * 0.25));
  const early = average(values.slice(0, chunk));
  const late = average(values.slice(-chunk));
  return {
    early,
    late,
    delta: early - late,
  };
}

/**
 * @param {any[]} snapshots
 * @returns {{tailRatio:number,maxTail:number,minTail:number}}
 */
function tailEligibleHealth(snapshots) {
  if (!snapshots || snapshots.length === 0) {
    return { tailRatio: 0, maxTail: 0, minTail: 0 };
  }

  const chunk = Math.max(3, Math.floor(snapshots.length * 0.2));
  const tail = snapshots
    .slice(-chunk)
    .map((item) => Number(item.eligibleCount || 0));
  const maxTail = Math.max(...tail, 0);
  const minTail = Math.min(...tail);

  return {
    tailRatio: maxTail > 0 ? minTail / maxTail : 0,
    maxTail,
    minTail,
  };
}

/**
 * @param {{snapshots:any[],finalSnapshot:any}} run
 * @returns {{score:number,verdict:string,metrics:Record<string,number|string|boolean>,rationale:string[]}}
 */
export function scoreStability(run) {
  const snapshots = run.snapshots || [];
  const totalRounds = snapshots.length;

  if (totalRounds === 0) {
    return {
      score: 0,
      verdict: "UNSTABLE",
      metrics: {
        totalRounds: 0,
      },
      rationale: ["No rounds executed."],
    };
  }

  const theoremPassRate =
    snapshots.filter((item) => item.theoremPass === true).length / totalRounds;

  const assumptionsPassRate =
    snapshots.filter((item) => item.assumptionsPass === true).length /
    totalRounds;

  const coverage = snapshots.map((item) => Number(item.downCoverage || 0));
  const coverageAvg = average(coverage);

  const duplicates = snapshots.map((item) => Number(item.downDuplicates || 0));
  const duplicateTrend = trendDelta(duplicates);

  const parentChanges = snapshots.map((item) =>
    Number(item.parentChanges || 0),
  );
  const parentChangeAvg = average(parentChanges);

  const flapping = snapshots.map((item) => Number(item.flappingNodes || 0));
  const flappingAvg = average(flapping);

  const eligibleHealth = tailEligibleHealth(snapshots);

  const theoremScore = theoremPassRate * 30;
  const assumptionsScore = assumptionsPassRate * 18;
  const coverageScore = Math.min(1, coverageAvg / 0.95) * 18;
  const duplicateScore =
    duplicateTrend.delta > 0
      ? Math.min(1, duplicateTrend.delta / Math.max(1, duplicateTrend.early)) *
        16
      : 0;

  const parentScore =
    Math.max(0, 10 - Math.min(10, parentChangeAvg)) +
    Math.max(0, 8 - Math.min(8, flappingAvg * 1.5));

  let score =
    theoremScore +
    assumptionsScore +
    coverageScore +
    duplicateScore +
    parentScore;

  const rationale = [];

  if (eligibleHealth.tailRatio < 0.6) {
    score *= 0.7;
    rationale.push("Eligible-set collapse detected in tail rounds.");
  }

  if (
    snapshots.slice(-8).every((item) => Number(item.eligibleCount || 0) <= 1)
  ) {
    score *= 0.45;
    rationale.push(
      "Vacuous tail: only gateway remains eligible in final rounds.",
    );
  }

  if (flappingAvg > 2.2) {
    score *= 0.82;
    rationale.push("High average flapping detected.");
  }

  score = Math.max(0, Math.min(100, score));

  const verdict =
    score >= 80 ? "STABLE" : score >= 60 ? "OSCILLATING" : "UNSTABLE";

  return {
    score,
    verdict,
    metrics: {
      totalRounds,
      theoremPassRate,
      assumptionsPassRate,
      coverageAvg,
      duplicateEarly: duplicateTrend.early,
      duplicateLate: duplicateTrend.late,
      duplicateDrop: duplicateTrend.delta,
      parentChangeAvg,
      flappingAvg,
      eligibleTailRatio: eligibleHealth.tailRatio,
      eligibleTailMin: eligibleHealth.minTail,
      eligibleTailMax: eligibleHealth.maxTail,
    },
    rationale,
  };
}

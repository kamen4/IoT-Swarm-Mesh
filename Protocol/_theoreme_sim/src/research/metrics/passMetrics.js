/* Purpose: Build compact theorem/assumption pass metrics from per-round snapshots. */

/**
 * @param {any[]} snapshots
 * @param {string} key
 * @returns {number}
 */
function countPass(snapshots, key) {
  return (snapshots || []).reduce(
    (sum, item) => sum + (item?.[key] === true ? 1 : 0),
    0,
  );
}

/**
 * @param {number} count
 * @param {number} total
 * @returns {number}
 */
function ratio(count, total) {
  if (!Number.isFinite(total) || total <= 0) {
    return 0;
  }
  return count / total;
}

/**
 * @param {any[]} snapshots
 * @param {string} key
 * @returns {number}
 */
function averageValue(snapshots, key) {
  const values = (snapshots || []).map((item) => Number(item?.[key] || 0));
  if (values.length === 0) {
    return 0;
  }
  return values.reduce((sum, value) => sum + value, 0) / values.length;
}

/**
 * @param {any[]} snapshots
 * @returns {{
 *  rounds:number,
 *  pendingRounds:number,
 *  theoremPassCount:number,
 *  theoremPassRate:number,
 *  assumptionsPassCount:number,
 *  assumptionsPassRate:number,
 *  a5PassCount:number,
 *  a5PassRate:number,
 *  a6PassCount:number,
 *  a6PassRate:number,
 *  a7PassCount:number,
 *  a7PassRate:number,
 *  lemma41PassCount:number,
 *  lemma41PassRate:number,
 *  lemma42PassCount:number,
 *  lemma42PassRate:number,
 *  lemma43PassCount:number,
 *  lemma43PassRate:number,
 *  avgEligibleCount:number,
 *  avgParentChanges:number,
 *  avgFlappingNodes:number,
 * }}
 */
export function buildCompactPassMetrics(snapshots) {
  const source = snapshots || [];
  const rounds = source.length;

  const theoremPassCount = countPass(source, "theoremPass");
  const assumptionsPassCount = countPass(source, "assumptionsPass");
  const a5PassCount = countPass(source, "a5");
  const a6PassCount = countPass(source, "a6");
  const a7PassCount = countPass(source, "a7");
  const lemma41PassCount = countPass(source, "lemma41");
  const lemma42PassCount = countPass(source, "lemma42");
  const lemma43PassCount = countPass(source, "lemma43");

  const pendingRounds = source.reduce(
    (sum, item) => sum + (item?.verificationState === "pending" ? 1 : 0),
    0,
  );

  return {
    rounds,
    pendingRounds,
    theoremPassCount,
    theoremPassRate: ratio(theoremPassCount, rounds),
    assumptionsPassCount,
    assumptionsPassRate: ratio(assumptionsPassCount, rounds),
    a5PassCount,
    a5PassRate: ratio(a5PassCount, rounds),
    a6PassCount,
    a6PassRate: ratio(a6PassCount, rounds),
    a7PassCount,
    a7PassRate: ratio(a7PassCount, rounds),
    lemma41PassCount,
    lemma41PassRate: ratio(lemma41PassCount, rounds),
    lemma42PassCount,
    lemma42PassRate: ratio(lemma42PassCount, rounds),
    lemma43PassCount,
    lemma43PassRate: ratio(lemma43PassCount, rounds),
    avgEligibleCount: averageValue(source, "eligibleCount"),
    avgParentChanges: averageValue(source, "parentChanges"),
    avgFlappingNodes: averageValue(source, "flappingNodes"),
  };
}

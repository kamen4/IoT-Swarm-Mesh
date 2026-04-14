/* Purpose: Convert per-round snapshots into CSV rows for external analysis. */

const CSV_COLUMNS = [
  "round",
  "verificationState",
  "eligibleCount",
  "assumptionsPass",
  "theoremPass",
  "a5",
  "a6",
  "a7",
  "lemma41",
  "lemma42",
  "lemma43",
  "stableRounds",
  "propagationUpdates",
  "propagationDeliveries",
  "downDuplicates",
  "downReachedCount",
  "downCoverage",
  "upAttempted",
  "upReachedGateway",
  "upHops",
  "upUpdates",
  "decayTriggered",
  "decayEpoch",
  "decayPercent",
  "parentChanges",
  "flappingNodes",
  "maxFlips",
  "spreadUpdates",
];

/**
 * @param {any} value
 * @returns {string}
 */
function cell(value) {
  const text = String(value ?? "");
  const escaped = text.replace(/"/g, '""');
  return `"${escaped}"`;
}

/**
 * @param {any[]} snapshots
 * @returns {string}
 */
export function toCsvExport(snapshots) {
  const lines = [CSV_COLUMNS.join(",")];

  for (const row of snapshots) {
    lines.push(CSV_COLUMNS.map((column) => cell(row[column])).join(","));
  }

  return lines.join("\n");
}

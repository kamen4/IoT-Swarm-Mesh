/* Purpose: Build compact chart and pass-diagnostic report data from research output. */

import { buildCompactPassMetrics } from "./metrics/passMetrics.js";

/**
 * @param {any[]} runs
 * @returns {any[]}
 */
function compactRuns(runs) {
  return (runs || []).map((run) => ({
    networkId: run.networkId,
    score: Number(run.score || 0),
    verdict: run.verdict,
    params: run.params || {},
  }));
}

/**
 * @param {any[]} runs
 * @param {string[]} parameterKeys
 * @returns {Record<string,Array<{x:number,y:number,verdict:string}>>}
 */
function buildDependencyData(runs, parameterKeys) {
  const params = parameterKeys || [];

  const result = {};

  for (const param of params) {
    result[param] = runs.map((run) => ({
      x: Number(run.params?.[param] ?? 0),
      y: Number(run.score ?? 0),
      verdict: run.verdict,
    }));
  }

  return result;
}

/**
 * @param {Array<Record<string,number>>} items
 * @param {string} key
 * @returns {number}
 */
function averageMetric(items, key) {
  if (!items || items.length === 0) {
    return 0;
  }

  const total = items.reduce((sum, item) => sum + Number(item?.[key] || 0), 0);
  return total / items.length;
}

/**
 * @param {any[]} snapshots
 * @returns {{duplicates:number[],coveragePercent:number[],eligibleCount:number[],parentChanges:number[],flappingNodes:number[]}}
 */
function buildChartSeries(snapshots) {
  const source = snapshots || [];

  return {
    duplicates: source.map((item) => Number(item.downDuplicates || 0)),
    coveragePercent: source.map((item) => Number(item.downCoverage || 0) * 100),
    eligibleCount: source.map((item) => Number(item.eligibleCount || 0)),
    parentChanges: source.map((item) => Number(item.parentChanges || 0)),
    flappingNodes: source.map((item) => Number(item.flappingNodes || 0)),
  };
}

/**
 * @param {any} study
 * @returns {any}
 */
export function buildReportData(study) {
  const runs = compactRuns(study.evaluationRuns);
  const tunedParameters = (study.tunedParameters || []).map((param) => ({
    key: param.key,
    label: param.label,
  }));
  const parameterKeys = tunedParameters.map((item) => item.key);

  const recommendations = (study.networks || []).map((network) => ({
    networkId: network.id,
    label: network.label,
    nodeCount: network.nodeCount,
    linkRadius: network.linkRadius,
    optimizer: "Adaptive gradient search + plateau escape",
    avgScore: Number(network.best?.avgScore || 0),
    stableRatio: Number(network.best?.stableRatio || 0),
    bestSeed: network.best?.seed,
    verdict: network.best?.verdict || "UNSTABLE",
    bestParameters: network.best?.parameters || {},
  }));

  const networkPassMetrics = (study.networks || []).map((network) =>
    buildCompactPassMetrics(network?.best?.snapshots || []),
  );

  const passSummary = {
    avgTheoremPassRate: averageMetric(networkPassMetrics, "theoremPassRate"),
    avgAssumptionsPassRate: averageMetric(
      networkPassMetrics,
      "assumptionsPassRate",
    ),
    avgA5PassRate: averageMetric(networkPassMetrics, "a5PassRate"),
    avgA6PassRate: averageMetric(networkPassMetrics, "a6PassRate"),
    avgA7PassRate: averageMetric(networkPassMetrics, "a7PassRate"),
    avgLemma41PassRate: averageMetric(networkPassMetrics, "lemma41PassRate"),
    avgLemma42PassRate: averageMetric(networkPassMetrics, "lemma42PassRate"),
    avgLemma43PassRate: averageMetric(networkPassMetrics, "lemma43PassRate"),
  };

  const metadata = {
    generatedAt: study?.metadata?.generatedAt,
    totalRuns: Number(study?.metadata?.totalRuns ?? runs.length),
    networkCount: Number(
      study?.metadata?.topologyCount ?? (study.networks || []).length,
    ),
    optimizationIterations: Number(
      study?.metadata?.optimizationIterations ?? 0,
    ),
    tunedParameterCount: tunedParameters.length,
    seedStart: Number(study?.metadata?.seedStart ?? 0),
    seedCount: Number(study?.metadata?.seedCount ?? 0),
    roundsPerCheck: Number(study?.metadata?.roundsPerCheck ?? 0),
    passSummary,
  };

  const matrix = recommendations.map((row) => ({
    networkId: row.networkId,
    label: row.label,
    nodeCount: row.nodeCount,
    linkRadius: row.linkRadius,
    avgScore: row.avgScore,
    stableRatio: row.stableRatio,
    verdict: row.verdict,
    bestSeed: row.bestSeed,
  }));

  const networkDetails = (study.networks || []).map((network) => {
    const best = network.best || {};
    const passMetrics = buildCompactPassMetrics(best.snapshots || []);

    return {
      id: network.id,
      label: network.label,
      nodeCount: network.nodeCount,
      linkRadius: network.linkRadius,
      bestAvgScore: Number(best.avgScore || 0),
      bestStableRatio: Number(best.stableRatio || 0),
      bestRunSeed: best.seed,
      bestRunVerdict: best.verdict,
      bestParameters: best.parameters || {},
      chartSeries: buildChartSeries(best.snapshots || []),
      passMetrics,
      scoreMetrics: best.scoreMetrics || null,
      scoreRationale: best.scoreRationale || [],
      topology: best.topology || null,
      optimizationTrace: network.optimizationTrace || [],
    };
  });

  return {
    metadata,
    tunedParameters,
    matrix,
    recommendations,
    networkDetails,
    dependencies: buildDependencyData(runs, parameterKeys),
  };
}

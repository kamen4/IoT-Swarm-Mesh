/* Purpose: Run batch no-UI simulations across topology/parameter/seed grids for stability research. */

import { normalizeConfig } from "../config/configNormalizer.js";
import { createSeededRng } from "../core/random.js";
import { runNoUiSimulation } from "../state/noUiSimulationRunner.js";
import { buildTopologyMatrix } from "./networkMatrixParser.js";
import {
  OPTIMIZATION_PARAMETERS,
  clampVector,
  decodeOptimizationVector,
  encodeOptimizationVector,
  randomDirection,
  summarizeOptimizationParameters,
} from "./parameterSearchSpace.js";
import {
  createOptimizationCandidate,
  hasMeaningfulOptimizationGain,
  nextOptimizationStep,
  optimizationPreference,
  sortByOptimizationPreferenceDesc,
} from "./optimization/candidateSelection.js";
import { scoreStability } from "./stabilityScorer.js";

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
 * @param {Array<{score:number}>} items
 * @returns {number}
 */
function averageScore(items) {
  if (!items || items.length === 0) {
    return 0;
  }
  return (
    items.reduce((sum, item) => sum + Number(item.score || 0), 0) / items.length
  );
}

/**
 * @param {number[]} values
 * @returns {number}
 */
function standardDeviation(values) {
  if (!values || values.length === 0) {
    return 0;
  }

  const mean = values.reduce((sum, value) => sum + Number(value || 0), 0);
  const average = mean / values.length;
  const variance =
    values.reduce((sum, value) => {
      const delta = Number(value || 0) - average;
      return sum + delta * delta;
    }, 0) / values.length;

  return Math.sqrt(variance);
}

/**
 * @returns {Promise<void>}
 */
function yieldToUi() {
  return new Promise((resolve) => {
    window.setTimeout(resolve, 0);
  });
}

/**
 * @param {number[]} vector
 * @param {number[]} direction
 * @param {number} distance
 * @returns {number[]}
 */
function moveVector(vector, direction, distance) {
  return clampVector(
    vector.map((value, index) => value + direction[index] * distance),
  );
}

/**
 * @param {number} avgScore
 * @param {number} stableRatio
 * @returns {string}
 */
function aggregateVerdict(avgScore, stableRatio) {
  if (stableRatio >= 0.66 || avgScore >= 82) {
    return "STABLE";
  }
  if (avgScore >= 60) {
    return "OSCILLATING";
  }
  return "UNSTABLE";
}

/**
 * @param {any} state
 * @returns {{nodes:Array<{id:number,x:number,y:number,isGateway:boolean}>,edges:number[][]}}
 */
function compactTopology(state) {
  return {
    nodes: [...state.nodes.values()].map((node) => ({
      id: node.id,
      x: Number(node.x),
      y: Number(node.y),
      isGateway: Boolean(node.isGateway),
    })),
    edges: (state.edges || []).map((edge) => [edge.a, edge.b]),
  };
}

/**
 * @param {{
 *  vector:number[],
 *  baseConfig:any,
 *  topology:{id:string,nodeCount:number,linkRadius:number},
 *  seedStart:number,
 *  seedCount:number,
 *  roundsPerCheck:number,
 *  stageLabel:string,
 *  progress:{completed:number,total:number,yieldEveryRuns:number},
 *  evaluationRuns:any[],
 *  onProgress?: (info:{completed:number,total:number,networkId?:string,stageId?:string,candidateId?:string,seed?:number}) => void,
 *  captureArtifacts:boolean,
 * }} input
 * @returns {Promise<any>}
 */
async function evaluateVector(input) {
  const configTemplate = decodeOptimizationVector(
    input.vector,
    input.baseConfig,
  );
  const runs = [];
  const scoreSamples = [];
  let tailEligibleRatioSum = 0;
  let flappingAvgSum = 0;

  let bestSeedRun = null;

  for (let seedOffset = 0; seedOffset < input.seedCount; seedOffset += 1) {
    const seed = input.seedStart + seedOffset;

    const config = normalizeConfig({
      ...configTemplate,
      nodeCount: input.topology.nodeCount,
      linkRadius: input.topology.linkRadius,
      seed,
      maxRounds: input.roundsPerCheck,
    });

    const simulation = runNoUiSimulation(config, input.roundsPerCheck);
    const scored = scoreStability(simulation);

    const runRecord = {
      seed,
      score: scored.score,
      verdict: scored.verdict,
      config,
    };

    if (input.captureArtifacts) {
      runRecord.scoreMetrics = scored.metrics;
      runRecord.scoreRationale = scored.rationale;
      runRecord.snapshots = simulation.snapshots;
      runRecord.state = simulation.state;
    }

    runs.push(runRecord);
    scoreSamples.push(Number(scored.score || 0));
    tailEligibleRatioSum += Number(scored.metrics?.eligibleTailRatio || 0);
    flappingAvgSum += Number(scored.metrics?.flappingAvg || 0);

    input.evaluationRuns.push({
      networkId: input.topology.id,
      score: scored.score,
      verdict: scored.verdict,
      params: summarizeOptimizationParameters(config),
    });

    input.progress.completed += 1;

    if (typeof input.onProgress === "function") {
      input.onProgress({
        completed: input.progress.completed,
        total: input.progress.total,
        networkId: input.topology.id,
        stageId: input.stageLabel,
        candidateId: input.stageLabel,
        seed,
      });
    }

    if (
      input.progress.completed % input.progress.yieldEveryRuns === 0 ||
      input.progress.completed === input.progress.total
    ) {
      await yieldToUi();
    }
  }

  const avgScore = averageScore(runs);
  const stableRatio =
    runs.filter((item) => item.verdict === "STABLE").length / runs.length;
  const tailEligibleRatio = tailEligibleRatioSum / Math.max(1, runs.length);
  const flappingAvg = flappingAvgSum / Math.max(1, runs.length);
  const scoreStdDev = standardDeviation(scoreSamples);

  if (input.captureArtifacts) {
    bestSeedRun = [...runs].sort((a, b) => b.score - a.score)[0] || null;
  }

  return {
    avgScore,
    stableRatio,
    tailEligibleRatio,
    flappingAvg,
    scoreStdDev,
    verdict: aggregateVerdict(avgScore, stableRatio),
    configTemplate,
    bestSeedRun,
  };
}

/**
 * @param {{
 *  baseConfig:any,
 *  seedStart?:number,
 *  seedCount?:number,
 *  roundsPerCheck?:number,
 *  matrixText?:string,
 *  nodeCountMin?:number,
 *  nodeCountMax?:number,
 *  nodeCountStep?:number,
 *  linkRadiusMin?:number,
 *  linkRadiusMax?:number,
 *  linkRadiusStep?:number,
 *  optimizationIterations?:number,
 *  yieldEveryRuns?:number,
 *  onProgress?:(info:{completed:number,total:number,networkId?:string,stageId?:string,candidateId?:string,seed?:number}) => void,
 * }} request
 * @returns {Promise<any>}
 */
export async function runBatchResearchStudy(request) {
  const baseConfig = normalizeConfig(request.baseConfig || {});

  const seedStart = clampInt(
    request.seedStart ?? baseConfig.seed,
    1,
    9_999_999,
  );
  const seedCount = clampInt(request.seedCount ?? 3, 1, 20);
  const roundsPerCheck = clampInt(
    request.roundsPerCheck ?? baseConfig.maxRounds,
    20,
    20_000,
  );

  const topologyMatrix = buildTopologyMatrix({
    matrixText: request.matrixText || "",
    nodeCountMin:
      request.nodeCountMin ?? Math.max(8, baseConfig.nodeCount - 12),
    nodeCountMax: request.nodeCountMax ?? baseConfig.nodeCount + 12,
    nodeCountStep: request.nodeCountStep ?? 8,
    linkRadiusMin:
      request.linkRadiusMin ?? Math.max(60, baseConfig.linkRadius - 30),
    linkRadiusMax: request.linkRadiusMax ?? baseConfig.linkRadius + 30,
    linkRadiusStep: request.linkRadiusStep ?? 20,
  });

  const optimizationIterations = clampInt(
    request.optimizationIterations ?? 12,
    3,
    40,
  );

  const runsPerTopology = seedCount * (2 + optimizationIterations * 3);
  const totalRuns = topologyMatrix.length * runsPerTopology;
  const yieldEveryRuns = clampInt(request.yieldEveryRuns ?? 1, 1, 50);
  const progress = {
    completed: 0,
    total: totalRuns,
    yieldEveryRuns,
  };

  if (typeof request.onProgress === "function") {
    request.onProgress({
      completed: 0,
      total: totalRuns,
    });
    await yieldToUi();
  }

  const evaluationRuns = [];
  const networks = [];

  for (const topology of topologyMatrix) {
    const rng = createSeededRng(
      seedStart + topology.nodeCount * 4099 + topology.linkRadius * 131,
    );

    let vector = encodeOptimizationVector(baseConfig);
    let stepSize = 0.44;
    const minStep = 0.02;
    let plateauStreak = 0;

    const optimizationTrace = [];

    const baseEvaluation = await evaluateVector({
      vector,
      baseConfig,
      topology,
      seedStart,
      seedCount,
      roundsPerCheck,
      stageLabel: "baseline",
      progress,
      evaluationRuns,
      onProgress: request.onProgress,
      captureArtifacts: false,
    });

    let current = createOptimizationCandidate({
      mode: "baseline",
      vector,
      score: baseEvaluation.avgScore,
      stableRatio: baseEvaluation.stableRatio,
      tailEligibleRatio: baseEvaluation.tailEligibleRatio,
      flappingAvg: baseEvaluation.flappingAvg,
      scoreStdDev: baseEvaluation.scoreStdDev,
    });

    let best = createOptimizationCandidate({
      mode: "baseline",
      vector,
      score: baseEvaluation.avgScore,
      stableRatio: baseEvaluation.stableRatio,
      tailEligibleRatio: baseEvaluation.tailEligibleRatio,
      flappingAvg: baseEvaluation.flappingAvg,
      scoreStdDev: baseEvaluation.scoreStdDev,
    });

    optimizationTrace.push({
      iteration: 0,
      mode: "baseline",
      stepSize,
      currentScore: current.score,
      currentObjective: current.objective,
      currentTailEligibleRatio: current.tailEligibleRatio,
      currentFlappingAvg: current.flappingAvg,
      currentScoreStdDev: current.scoreStdDev,
      bestScore: best.score,
      bestObjective: best.objective,
      stableRatio: current.stableRatio,
      plateauStreak,
    });

    for (
      let iteration = 1;
      iteration <= optimizationIterations;
      iteration += 1
    ) {
      const activeDimensions = best.score < 55 ? 6 : best.score < 68 ? 4 : 3;
      const direction = randomDirection(rng, activeDimensions);
      const plateauBoost =
        plateauStreak >= 3 ? 1 + Math.min(1.2, (plateauStreak - 2) * 0.22) : 1;
      const badStateBoost = best.score < 55 ? 1.55 : best.score < 68 ? 1.2 : 1;
      const probeDistance = Math.max(
        minStep,
        stepSize * badStateBoost * plateauBoost,
      );

      const plusVector = moveVector(vector, direction, probeDistance);
      const minusVector = moveVector(vector, direction, -probeDistance);

      const plusEvaluation = await evaluateVector({
        vector: plusVector,
        baseConfig,
        topology,
        seedStart,
        seedCount,
        roundsPerCheck,
        stageLabel: `iter-${iteration}-plus`,
        progress,
        evaluationRuns,
        onProgress: request.onProgress,
        captureArtifacts: false,
      });

      const minusEvaluation = await evaluateVector({
        vector: minusVector,
        baseConfig,
        topology,
        seedStart,
        seedCount,
        roundsPerCheck,
        stageLabel: `iter-${iteration}-minus`,
        progress,
        evaluationRuns,
        onProgress: request.onProgress,
        captureArtifacts: false,
      });

      const plusCandidate = createOptimizationCandidate({
        mode: "plus",
        vector: plusVector,
        score: plusEvaluation.avgScore,
        stableRatio: plusEvaluation.stableRatio,
        tailEligibleRatio: plusEvaluation.tailEligibleRatio,
        flappingAvg: plusEvaluation.flappingAvg,
        scoreStdDev: plusEvaluation.scoreStdDev,
      });

      const minusCandidate = createOptimizationCandidate({
        mode: "minus",
        vector: minusVector,
        score: minusEvaluation.avgScore,
        stableRatio: minusEvaluation.stableRatio,
        tailEligibleRatio: minusEvaluation.tailEligibleRatio,
        flappingAvg: minusEvaluation.flappingAvg,
        scoreStdDev: minusEvaluation.scoreStdDev,
      });

      const gradientDirection =
        optimizationPreference(plusCandidate, minusCandidate) >= 0 ? 1 : -1;
      const gradientMagnitude = Math.abs(
        plusCandidate.objective - minusCandidate.objective,
      );

      const moveStrength = Math.max(
        minStep,
        stepSize * (0.65 + Math.min(1.45, gradientMagnitude / 12)),
      );

      const gradientVector = moveVector(
        vector,
        direction,
        gradientDirection * moveStrength,
      );

      const gradientEvaluation = await evaluateVector({
        vector: gradientVector,
        baseConfig,
        topology,
        seedStart,
        seedCount,
        roundsPerCheck,
        stageLabel: `iter-${iteration}-gradient`,
        progress,
        evaluationRuns,
        onProgress: request.onProgress,
        captureArtifacts: false,
      });

      const gradientCandidate = createOptimizationCandidate({
        mode: "gradient",
        vector: gradientVector,
        score: gradientEvaluation.avgScore,
        stableRatio: gradientEvaluation.stableRatio,
        tailEligibleRatio: gradientEvaluation.tailEligibleRatio,
        flappingAvg: gradientEvaluation.flappingAvg,
        scoreStdDev: gradientEvaluation.scoreStdDev,
      });

      const options = [
        createOptimizationCandidate({
          mode: "hold",
          vector,
          score: current.score,
          stableRatio: current.stableRatio,
          tailEligibleRatio: current.tailEligibleRatio,
          flappingAvg: current.flappingAvg,
          scoreStdDev: current.scoreStdDev,
        }),
        plusCandidate,
        minusCandidate,
        gradientCandidate,
      ];

      options.sort(sortByOptimizationPreferenceDesc);
      const winner = options[0];
      let selected = winner;
      const improved = hasMeaningfulOptimizationGain(winner, current, stepSize);
      let forcedExplore = false;

      if (!improved && plateauStreak >= 4 && winner.mode === "hold") {
        const exploratory = options.find((option) => option.mode !== "hold");
        if (
          exploratory &&
          optimizationPreference(exploratory, current) > -0.8
        ) {
          selected = {
            ...exploratory,
            mode: `explore-${exploratory.mode}`,
          };
          forcedExplore = true;
        }
      }

      const moved = improved || forcedExplore;
      const objectiveDelta = selected.objective - current.objective;

      if (moved) {
        vector = selected.vector;
        current = selected;
        plateauStreak = 0;
      } else {
        plateauStreak += 1;
      }

      stepSize = nextOptimizationStep(
        stepSize,
        moved,
        objectiveDelta,
        minStep,
        current.stableRatio,
        plateauStreak,
      );

      if (optimizationPreference(winner, best) > 0) {
        best = winner;
      }

      optimizationTrace.push({
        iteration,
        mode: moved ? selected.mode : winner.mode,
        stepSize,
        currentScore: current.score,
        currentObjective: current.objective,
        currentTailEligibleRatio: current.tailEligibleRatio,
        currentFlappingAvg: current.flappingAvg,
        currentScoreStdDev: current.scoreStdDev,
        bestScore: best.score,
        bestObjective: best.objective,
        stableRatio: current.stableRatio,
        plateauStreak,
        activeDimensions,
      });
    }

    const bestDetailed = await evaluateVector({
      vector: best.vector,
      baseConfig,
      topology,
      seedStart,
      seedCount,
      roundsPerCheck,
      stageLabel: "best-final",
      progress,
      evaluationRuns,
      onProgress: request.onProgress,
      captureArtifacts: true,
    });

    const bestSeedRun = bestDetailed.bestSeedRun;
    const bestConfig = bestSeedRun?.config || bestDetailed.configTemplate;

    networks.push({
      ...topology,
      optimizationTrace,
      best: {
        avgScore: bestDetailed.avgScore,
        stableRatio: bestDetailed.stableRatio,
        verdict: aggregateVerdict(
          bestDetailed.avgScore,
          bestDetailed.stableRatio,
        ),
        objective: best.objective,
        seed: bestSeedRun?.seed ?? seedStart,
        config: bestConfig,
        parameters: summarizeOptimizationParameters(bestConfig),
        snapshots: bestSeedRun?.snapshots || [],
        scoreMetrics: bestSeedRun?.scoreMetrics || null,
        scoreRationale: bestSeedRun?.scoreRationale || [],
        topology: bestSeedRun?.state
          ? compactTopology(bestSeedRun.state)
          : null,
      },
    });
  }

  const recommendations = networks.map((network) => ({
    networkId: network.id,
    label: network.label,
    nodeCount: network.nodeCount,
    linkRadius: network.linkRadius,
    optimizer: "Adaptive gradient search + plateau escape",
    avgScore: network.best.avgScore,
    stableRatio: network.best.stableRatio,
    bestSeed: network.best.seed,
    verdict: network.best.verdict,
    bestParameters: network.best.parameters,
  }));

  return {
    metadata: {
      generatedAt: new Date().toISOString(),
      seedStart,
      seedCount,
      roundsPerCheck,
      totalRuns,
      topologyCount: topologyMatrix.length,
      optimizationIterations,
    },
    tunedParameters: OPTIMIZATION_PARAMETERS.map((item) => ({
      key: item.key,
      label: item.label,
      type: item.type,
      min: item.min,
      max: item.max,
    })),
    evaluationRuns,
    networks,
    recommendations,
  };
}

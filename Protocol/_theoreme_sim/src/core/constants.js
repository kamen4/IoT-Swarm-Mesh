/* Purpose: Centralized constants for theorem simulation defaults and rendering behavior. */

export const THEOREM_ROOT_ID = 0;

export const DEFAULT_CONFIG = {
  nodeCount: 34,
  linkRadius: 195,
  qForward: 220,
  deliveryProbability: 0.72,
  penaltyLambda: 28,
  switchHysteresis: 9,
  switchHysteresisRatio: 0.03,
  rootSourceCharge: 1500,
  chargeDropPerHop: 80,
  chargeSpreadFactor: 0.28,
  decayIntervalSteps: 60,
  decayPercent: 0.12,
  linkMemory: 0.94,
  linkLearningRate: 0.2,
  linkBonusMax: 45,
  seed: 42,
  roundsPerSecond: 3,
  maxRounds: 350,
  enforceTheoremAssumptions: false,
};

export const LAYOUT = {
  width: 1120,
  height: 760,
  margin: 52,
};

export const RENDERING = {
  nodeRadius: 11,
  treeEdgeBoost: 2.1,
  edgeLabelFont: "12px IBM Plex Mono, monospace",
  nodeLabelFont: "11px IBM Plex Mono, monospace",
};

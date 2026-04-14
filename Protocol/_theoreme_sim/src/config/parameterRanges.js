/* Purpose: Define simulation parameter ranges used by UI controls and full randomization. */

export const PARAMETER_RANGES = {
  nodeCount: { type: "int", min: 8, max: 180 },
  linkRadius: { type: "int", min: 60, max: 360 },
  qForward: { type: "int", min: 20, max: 1300 },
  deliveryProbability: { type: "float", min: 0.15, max: 1, precision: 2 },
  penaltyLambda: { type: "int", min: 0, max: 150 },
  switchHysteresis: { type: "int", min: 0, max: 120 },
  switchHysteresisRatio: { type: "float", min: 0, max: 0.2, precision: 2 },
  rootSourceCharge: { type: "int", min: 500, max: 2200 },
  chargeDropPerHop: { type: "int", min: 10, max: 260 },
  chargeSpreadFactor: { type: "float", min: 0.05, max: 1, precision: 2 },
  decayIntervalSteps: { type: "int", min: 0, max: 500 },
  decayPercent: { type: "float", min: 0, max: 0.4, precision: 2 },
  linkMemory: { type: "float", min: 0.75, max: 0.995, precision: 3 },
  linkLearningRate: { type: "float", min: 0.05, max: 0.8, precision: 2 },
  linkBonusMax: { type: "int", min: 0, max: 120 },
  seed: { type: "int", min: 1, max: 999999 },
  roundsPerSecond: { type: "int", min: 1, max: 30 },
  maxRounds: { type: "int", min: 20, max: 5000 },
  enforceTheoremAssumptions: { type: "boolean" },
};

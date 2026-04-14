/* Purpose: Build export-friendly snapshots from simulation state for JSON and CSV outputs. */

/**
 * @param {any} state
 * @returns {any}
 */
export function buildRoundSnapshot(state) {
  const report = state.lastTheoremReport;
  const broadcast = state.lastBroadcastReport;
  const up = state.lastUp ?? {
    attempted: 0,
    reachedGateway: 0,
    hops: 0,
    updates: 0,
  };
  const decay = state.lastDecay ?? {
    triggered: false,
    epoch: 0,
    percent: 0,
    factor: 1,
  };
  const oscillation = state.lastOscillationReport ?? {
    changedParents: 0,
    flappingNodes: 0,
    totalTracked: 0,
    maxFlips: 0,
  };

  return {
    round: state.round,
    verificationState: report.verificationState ?? "pending",
    eligibleCount: report.eligibleCount,
    assumptionsPass: report.assumptionsPass,
    theoremPass: report.theoremPass,
    a5: report.a5,
    a6: report.a6,
    a7: report.a7,
    lemma41: report.lemma41,
    lemma42: report.lemma42,
    lemma43: report.lemma43,
    stableRounds: state.stableRounds,
    propagationUpdates: state.lastPropagation?.updates ?? 0,
    propagationDeliveries: state.lastPropagation?.deliveries ?? 0,
    downDuplicates: broadcast?.duplicates ?? 0,
    downReachedCount: broadcast?.reachedCount ?? 0,
    downCoverage: broadcast?.coverage ?? 0,
    upAttempted: up.attempted,
    upReachedGateway: up.reachedGateway,
    upHops: up.hops,
    upUpdates: up.updates,
    decayTriggered: decay.triggered,
    decayEpoch: decay.epoch,
    decayPercent: decay.percent,
    parentChanges: oscillation.changedParents,
    flappingNodes: oscillation.flappingNodes,
    maxFlips: oscillation.maxFlips,
    spreadUpdates: state.lastSpread?.updates ?? 0,
  };
}

/**
 * @param {any} state
 * @param {any[]} snapshots
 * @param {number} stepsExecuted
 * @returns {any}
 */
export function buildFinalSnapshot(state, snapshots, stepsExecuted) {
  return {
    exportedAt: new Date().toISOString(),
    stepsExecuted,
    config: state.config,
    finalRound: state.round,
    theorem: state.lastTheoremReport,
    broadcast: state.lastBroadcastReport,
    broadcastHistory: state.broadcastHistory ?? [],
    decayHistory: state.decayHistory ?? [],
    oscillation: state.lastOscillationReport,
    up: state.lastUp,
    nodes: [...state.nodes.values()].map((node) => ({
      id: node.id,
      qTotal: node.qTotal,
      eligible: node.eligible,
      parent: node.parent,
      children: node.children,
    })),
    snapshots,
  };
}

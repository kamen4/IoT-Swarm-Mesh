/* Purpose: Generate a standalone HTML report with embedded data, charts, and JSON/CSV download actions. */

/**
 * @param {any} value
 * @returns {string}
 */
function htmlEscape(value) {
  return String(value)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/\"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

/**
 * @param {any} reportData
 * @returns {string}
 */
export function buildResearchReportHtml(reportData) {
  const payload = JSON.stringify(reportData).replace(/</g, "\\u003c");

  return `<!doctype html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>Research Report ${htmlEscape(reportData.metadata.generatedAt)}</title>
  <style>
    :root {
      --bg: #f5f6f2;
      --panel: #ffffff;
      --line: #d9ddcf;
      --text: #1f2926;
      --muted: #50615b;
      --accent: #0b7668;
      --warn: #b95a1f;
      --ok: #137a3d;
      --bad: #af2f1f;
    }
    * { box-sizing: border-box; }
    body {
      margin: 0;
      font-family: "Segoe UI", Tahoma, sans-serif;
      color: var(--text);
      background: radial-gradient(circle at 15% 10%, #eef7f1, transparent 40%), var(--bg);
    }
    .shell {
      max-width: 1480px;
      margin: 0 auto;
      padding: 16px;
      display: grid;
      gap: 12px;
    }
    .panel {
      background: var(--panel);
      border: 1px solid var(--line);
      border-radius: 12px;
      padding: 12px;
      box-shadow: 0 7px 24px rgba(8, 32, 26, 0.07);
    }
    h1,h2,h3 { margin: 0 0 8px; }
    .meta {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 8px;
      color: var(--muted);
      font-size: 0.92rem;
    }
    .grid-2 {
      display: grid;
      grid-template-columns: 1.2fr 1fr;
      gap: 12px;
    }
    .grid-3 {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 12px;
    }
    .grid-4 {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 12px;
    }
    canvas {
      width: 100%;
      min-height: 220px;
      border: 1px solid var(--line);
      border-radius: 8px;
      background: #fff;
      display: block;
    }
    table {
      width: 100%;
      border-collapse: collapse;
      font-size: 0.86rem;
    }
    th, td {
      border-bottom: 1px solid #e8ebdf;
      text-align: left;
      padding: 6px;
      vertical-align: top;
    }
    th { color: #28423b; }
    .pill {
      display: inline-block;
      padding: 2px 8px;
      border-radius: 999px;
      font-size: 0.75rem;
      border: 1px solid #dbe8e1;
      background: #f1faf6;
    }
    .stable { color: var(--ok); }
    .osc { color: var(--warn); }
    .unstable { color: var(--bad); }
    details {
      border: 1px solid var(--line);
      border-radius: 10px;
      padding: 8px;
      margin-bottom: 10px;
      background: #fcfdfb;
    }
    summary {
      cursor: pointer;
      font-weight: 600;
      color: #25453f;
      margin-bottom: 8px;
    }
    .network-top {
      display: grid;
      grid-template-columns: 1.2fr 1fr;
      gap: 12px;
      margin-bottom: 10px;
    }
    .export-actions {
      display: flex;
      gap: 10px;
      flex-wrap: wrap;
      margin-top: 8px;
    }
    .export-actions button {
      border: 1px solid #c8d9d0;
      background: #f0f8f4;
      color: #21443d;
      border-radius: 8px;
      padding: 7px 12px;
      cursor: pointer;
      font-weight: 600;
    }
    .export-note {
      color: var(--muted);
      margin-top: 8px;
      font-size: 0.86rem;
    }
    .param-table td:first-child {
      color: #395c55;
      font-family: Consolas, monospace;
      font-size: 0.8rem;
    }
    .metric-table {
      margin-top: 8px;
      font-size: 0.8rem;
    }
    .metric-table td:first-child {
      color: #395c55;
      width: 58%;
    }
    .rationale-list {
      margin: 8px 0 0;
      padding-left: 18px;
      color: #4d5f59;
      font-size: 0.8rem;
    }
    pre {
      white-space: pre-wrap;
      word-break: break-word;
      background: #f5f8f4;
      border: 1px solid var(--line);
      border-radius: 8px;
      padding: 10px;
      font-size: 0.78rem;
      max-height: 340px;
      overflow: auto;
    }
    @media (max-width: 1100px) {
      .grid-2, .grid-3, .grid-4, .network-top { grid-template-columns: 1fr; }
    }
  </style>
</head>
<body>
  <div class="shell">
    <section class="panel">
      <h1>Batch Stability Research Report</h1>
      <div class="meta" id="meta"></div>
    </section>

    <section class="panel grid-2">
      <div>
        <h2>Topology Stability Heatmap</h2>
        <canvas id="heatmap" width="840" height="340"></canvas>
      </div>
      <div>
        <h2>Recommendations</h2>
        <table>
          <thead>
            <tr>
              <th>Topology</th>
              <th>Method</th>
              <th>Score</th>
              <th>Stable Ratio</th>
              <th>Verdict</th>
            </tr>
          </thead>
          <tbody id="recommendations"></tbody>
        </table>
      </div>
    </section>

    <section class="panel">
      <h2>Embedded Downloads</h2>
      <div class="export-actions">
        <button type="button" data-action="download-json">Download JSON</button>
        <button type="button" data-action="download-csv">Download CSV</button>
      </div>
      <div class="export-note">Report export is HTML-only. Use these buttons to get JSON or CSV from embedded data.</div>
    </section>

    <section class="panel">
      <h2>Parameter Dependencies</h2>
      <div class="grid-3" id="dependency-grid"></div>
    </section>

    <section class="panel">
      <h2>Per-Network Details</h2>
      <div id="network-details"></div>
    </section>

  </div>

  <script>
    const REPORT = ${payload};

    function verdictClass(verdict) {
      if (verdict === 'STABLE') return 'stable';
      if (verdict === 'OSCILLATING') return 'osc';
      return 'unstable';
    }

    function setMeta() {
      const root = document.getElementById('meta');
      const m = REPORT.metadata;
      const pass = m.passSummary || {};
      root.innerHTML = [
        '<div><strong>Generated:</strong> ' + m.generatedAt + '</div>',
        '<div><strong>Total runs:</strong> ' + m.totalRuns + '</div>',
        '<div><strong>Networks:</strong> ' + m.networkCount + '</div>',
        '<div><strong>Optimization iterations:</strong> ' + m.optimizationIterations + '</div>',
        '<div><strong>Tuned parameters:</strong> ' + m.tunedParameterCount + '</div>',
        '<div><strong>Seed start:</strong> ' + m.seedStart + '</div>',
        '<div><strong>Seeds per check:</strong> ' + m.seedCount + '</div>',
        '<div><strong>Rounds per check:</strong> ' + m.roundsPerCheck + '</div>',
        '<div><strong>Avg theorem pass:</strong> ' + toPercent(pass.avgTheoremPassRate) + '</div>',
        '<div><strong>Avg assumptions pass:</strong> ' + toPercent(pass.avgAssumptionsPassRate) + '</div>',
        '<div><strong>Avg A5/A6/A7 pass:</strong> ' + [
          toPercent(pass.avgA5PassRate),
          toPercent(pass.avgA6PassRate),
          toPercent(pass.avgA7PassRate),
        ].join(' / ') + '</div>',
      ].join('');
    }

    function renderRecommendations() {
      const tbody = document.getElementById('recommendations');
      tbody.innerHTML = REPORT.recommendations.map((row) => {
        const score = Number(row.avgScore || 0).toFixed(1);
        const ratio = (Number(row.stableRatio || 0) * 100).toFixed(0) + '%';
        return '<tr>' +
          '<td>' + row.label + '</td>' +
          '<td>' + row.optimizer + '</td>' +
          '<td>' + score + '</td>' +
          '<td>' + ratio + '</td>' +
          '<td><span class="pill ' + verdictClass(row.verdict) + '">' + row.verdict + '</span></td>' +
          '</tr>';
      }).join('');
    }

    function toPercent(value) {
      return (Number(value || 0) * 100).toFixed(1) + '%';
    }

    function downloadTextFile(fileName, text, mimeType) {
      const blob = new Blob([text], { type: mimeType });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = fileName;
      document.body.appendChild(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);
    }

    function makeReportFilePrefix() {
      const generatedAt = REPORT?.metadata?.generatedAt || new Date().toISOString();
      return 'research-batch-report-' + generatedAt.replace(/[:.]/g, '-');
    }

    function csvCell(value) {
      return '"' + String(value ?? '').replace(/"/g, '""') + '"';
    }

    function recommendationsCsv() {
      const rows = [[
        'networkId',
        'label',
        'nodeCount',
        'linkRadius',
        'optimizer',
        'avgScore',
        'stableRatio',
        'bestSeed',
        'verdict',
      ]];

      for (const row of REPORT.recommendations || []) {
        rows.push([
          row.networkId,
          row.label,
          row.nodeCount,
          row.linkRadius,
          row.optimizer,
          Number(row.avgScore || 0).toFixed(3),
          Number(row.stableRatio || 0).toFixed(4),
          row.bestSeed,
          row.verdict,
        ]);
      }

      return rows
        .map((line) => line.map((cell) => csvCell(cell)).join(','))
        .join('\\n');
    }

    function bindEmbeddedDownloads() {
      const jsonButton = document.querySelector('[data-action="download-json"]');
      if (jsonButton) {
        jsonButton.addEventListener('click', () => {
          downloadTextFile(
            makeReportFilePrefix() + '.json',
            JSON.stringify(REPORT),
            'application/json;charset=utf-8',
          );
        });
      }

      const csvButton = document.querySelector('[data-action="download-csv"]');
      if (csvButton) {
        csvButton.addEventListener('click', () => {
          downloadTextFile(
            makeReportFilePrefix() + '.csv',
            recommendationsCsv(),
            'text/csv;charset=utf-8',
          );
        });
      }
    }

    function drawAxes(ctx, width, height, margin) {
      ctx.strokeStyle = '#9ab2a5';
      ctx.lineWidth = 1;
      ctx.beginPath();
      ctx.moveTo(margin.left, margin.top);
      ctx.lineTo(margin.left, height - margin.bottom);
      ctx.lineTo(width - margin.right, height - margin.bottom);
      ctx.stroke();
    }

    function drawHeatmap() {
      const canvas = document.getElementById('heatmap');
      const ctx = canvas.getContext('2d');
      const width = canvas.width;
      const height = canvas.height;
      ctx.clearRect(0, 0, width, height);

      const rows = REPORT.matrix || [];
      if (rows.length === 0) {
        ctx.fillText('No matrix data', 20, 20);
        return;
      }

      const nodes = [...new Set(rows.map((r) => r.nodeCount))].sort((a, b) => a - b);
      const radii = [...new Set(rows.map((r) => r.linkRadius))].sort((a, b) => a - b);

      const left = 88;
      const top = 28;
      const plotW = width - left - 22;
      const plotH = height - top - 46;
      const cellW = plotW / Math.max(1, radii.length);
      const cellH = plotH / Math.max(1, nodes.length);

      for (let ny = 0; ny < nodes.length; ny += 1) {
        for (let rx = 0; rx < radii.length; rx += 1) {
          const row = rows.find((item) => item.nodeCount === nodes[ny] && item.linkRadius === radii[rx]);
          const score = Number(row?.avgScore || 0);
          const t = Math.max(0, Math.min(1, score / 100));
          const r = Math.round(190 - 120 * t);
          const g = Math.round(70 + 130 * t);
          const b = Math.round(64 - 25 * t);

          ctx.fillStyle = 'rgb(' + r + ',' + g + ',' + b + ')';
          ctx.fillRect(left + rx * cellW, top + ny * cellH, cellW - 1, cellH - 1);

          if (row) {
            ctx.fillStyle = '#ffffff';
            ctx.font = '11px Segoe UI';
            ctx.fillText(score.toFixed(0), left + rx * cellW + 4, top + ny * cellH + 14);
          }
        }
      }

      ctx.fillStyle = '#2c4741';
      ctx.font = '12px Segoe UI';
      for (let i = 0; i < radii.length; i += 1) {
        ctx.fillText(String(radii[i]), left + i * cellW + 2, height - 18);
      }
      for (let i = 0; i < nodes.length; i += 1) {
        ctx.fillText(String(nodes[i]), 28, top + i * cellH + 14);
      }

      ctx.fillText('Link radius', width / 2 - 36, height - 2);
      ctx.save();
      ctx.translate(12, height / 2 + 28);
      ctx.rotate(-Math.PI / 2);
      ctx.fillText('Node count', 0, 0);
      ctx.restore();
    }

    function drawScatter(canvas, points, title, xLabel) {
      const ctx = canvas.getContext('2d');
      const width = canvas.width;
      const height = canvas.height;
      ctx.clearRect(0, 0, width, height);

      const margin = { left: 42, right: 14, top: 24, bottom: 30 };
      drawAxes(ctx, width, height, margin);

      if (!points || points.length === 0) {
        ctx.fillStyle = '#405a53';
        ctx.fillText('No data', margin.left + 10, margin.top + 10);
        return;
      }

      const minX = Math.min(...points.map((p) => p.x));
      const maxX = Math.max(...points.map((p) => p.x));
      const rangeX = Math.max(1e-9, maxX - minX);

      for (const p of points) {
        const x = margin.left + ((p.x - minX) / rangeX) * (width - margin.left - margin.right);
        const y = height - margin.bottom - (p.y / 100) * (height - margin.top - margin.bottom);

        ctx.fillStyle = p.verdict === 'STABLE' ? '#1f8a48' : p.verdict === 'OSCILLATING' ? '#bd6d20' : '#b13a2f';
        ctx.beginPath();
        ctx.arc(x, y, 3.5, 0, Math.PI * 2);
        ctx.fill();
      }

      ctx.fillStyle = '#2d4a43';
      ctx.font = '12px Segoe UI';
      ctx.fillText(title, margin.left, 14);
      ctx.fillText(xLabel, width / 2 - 24, height - 8);
      ctx.fillText('Score', 4, 16);
    }

    function renderDependencies() {
      const holder = document.getElementById('dependency-grid');
      const keys = Object.keys(REPORT.dependencies || {});

      for (const key of keys) {
        const panel = document.createElement('div');
        panel.className = 'panel';

        const canvas = document.createElement('canvas');
        canvas.width = 420;
        canvas.height = 250;

        panel.appendChild(canvas);
        holder.appendChild(panel);

        drawScatter(
          canvas,
          REPORT.dependencies[key],
          key + ' vs stability score',
          key,
        );
      }
    }

    function drawLine(canvas, series, title) {
      const ctx = canvas.getContext('2d');
      const width = canvas.width;
      const height = canvas.height;
      ctx.clearRect(0, 0, width, height);

      const margin = { left: 36, right: 12, top: 22, bottom: 26 };
      drawAxes(ctx, width, height, margin);

      const maxLen = Math.max(...series.map((s) => s.values.length), 1);
      let maxVal = 1;
      for (const s of series) {
        for (const v of s.values) {
          maxVal = Math.max(maxVal, Number(v || 0));
        }
      }

      for (const s of series) {
        ctx.strokeStyle = s.color;
        ctx.lineWidth = 1.6;
        ctx.beginPath();

        for (let i = 0; i < s.values.length; i += 1) {
          const x = margin.left + (i / Math.max(1, maxLen - 1)) * (width - margin.left - margin.right);
          const y = height - margin.bottom - (Number(s.values[i] || 0) / maxVal) * (height - margin.top - margin.bottom);
          if (i === 0) {
            ctx.moveTo(x, y);
          } else {
            ctx.lineTo(x, y);
          }
        }

        ctx.stroke();
      }

      ctx.fillStyle = '#2d4a43';
      ctx.font = '12px Segoe UI';
      ctx.fillText(title, margin.left, 14);
    }

    function drawTopology(canvas, topology) {
      const ctx = canvas.getContext('2d');
      const width = canvas.width;
      const height = canvas.height;
      ctx.clearRect(0, 0, width, height);

      const nodes = topology?.nodes || [];
      const edges = topology?.edges || [];
      if (nodes.length === 0) {
        ctx.fillStyle = '#2d4a43';
        ctx.fillText('No topology data', 12, 18);
        return;
      }

      const map = new Map(nodes.map((node) => [node.id, node]));
      const minX = Math.min(...nodes.map((node) => Number(node.x || 0)));
      const maxX = Math.max(...nodes.map((node) => Number(node.x || 0)));
      const minY = Math.min(...nodes.map((node) => Number(node.y || 0)));
      const maxY = Math.max(...nodes.map((node) => Number(node.y || 0)));

      const margin = 16;
      const spanX = Math.max(1e-9, maxX - minX);
      const spanY = Math.max(1e-9, maxY - minY);
      const scale = Math.min(
        (width - margin * 2) / spanX,
        (height - margin * 2) / spanY,
      );

      function px(x) {
        return margin + (Number(x || 0) - minX) * scale;
      }

      function py(y) {
        return margin + (Number(y || 0) - minY) * scale;
      }

      ctx.strokeStyle = 'rgba(29, 78, 72, 0.18)';
      ctx.lineWidth = 1;
      for (const edge of edges) {
        const a = map.get(edge[0]);
        const b = map.get(edge[1]);
        if (!a || !b) {
          continue;
        }
        ctx.beginPath();
        ctx.moveTo(px(a.x), py(a.y));
        ctx.lineTo(px(b.x), py(b.y));
        ctx.stroke();
      }

      for (const node of nodes) {
        ctx.beginPath();
        ctx.arc(px(node.x), py(node.y), node.isGateway ? 4.8 : 3.2, 0, Math.PI * 2);
        ctx.fillStyle = node.isGateway ? '#bf5312' : '#1b6f65';
        ctx.fill();
      }

      ctx.fillStyle = '#2d4a43';
      ctx.font = '12px Segoe UI';
      ctx.fillText('Nodes: ' + nodes.length + ' | Edges: ' + edges.length, 10, 14);
    }

    function drawOptimizationTrace(canvas, trace) {
      const currentSeries = (trace || []).map((item) => Number(item.currentScore || 0));
      const bestSeries = (trace || []).map((item) => Number(item.bestScore || 0));

      drawLine(canvas, [
        { color: '#5f7a74', values: currentSeries },
        { color: '#0e6d62', values: bestSeries },
      ], 'Optimization trajectory (current/best score)');
    }

    function renderParametersTable(network) {
      const table = document.createElement('table');
      table.className = 'param-table';

      const keys = REPORT.tunedParameters || [];
      const rows = keys.map((item) => {
        const value = network.bestParameters?.[item.key];
        return '<tr><td>' + item.label + '</td><td>' + (value ?? '-') + '</td></tr>';
      }).join('');

      table.innerHTML = '<thead><tr><th>Parameter</th><th>Best value</th></tr></thead><tbody>' + rows + '</tbody>';
      return table;
    }

    function renderPassMetricsTable(passMetrics) {
      const metrics = passMetrics || {};
      const table = document.createElement('table');
      table.className = 'metric-table';

      table.innerHTML = '<thead><tr><th>Metric</th><th>Value</th></tr></thead><tbody>' + [
        ['Theorem PASS', toPercent(metrics.theoremPassRate)],
        ['Assumptions PASS', toPercent(metrics.assumptionsPassRate)],
        ['A5 / A6 / A7 PASS', [
          toPercent(metrics.a5PassRate),
          toPercent(metrics.a6PassRate),
          toPercent(metrics.a7PassRate),
        ].join(' / ')],
        ['Lemma 4.1 / 4.2 / 4.3 PASS', [
          toPercent(metrics.lemma41PassRate),
          toPercent(metrics.lemma42PassRate),
          toPercent(metrics.lemma43PassRate),
        ].join(' / ')],
        ['Pending rounds', Number(metrics.pendingRounds || 0)],
      ].map((row) => '<tr><td>' + row[0] + '</td><td>' + row[1] + '</td></tr>').join('') + '</tbody>';

      return table;
    }

    function renderScoreMetricsTable(scoreMetrics, rationale) {
      const metrics = scoreMetrics || {};
      const table = document.createElement('table');
      table.className = 'metric-table';

      table.innerHTML = '<thead><tr><th>Score component</th><th>Value</th></tr></thead><tbody>' + [
        ['Theorem pass rate', toPercent(metrics.theoremPassRate)],
        ['Assumptions pass rate', toPercent(metrics.assumptionsPassRate)],
        ['Coverage avg', toPercent(metrics.coverageAvg)],
        ['Duplicate drop', Number(metrics.duplicateDrop || 0).toFixed(2)],
        ['Tail eligible ratio', Number(metrics.eligibleTailRatio || 0).toFixed(3)],
        ['Parent changes avg', Number(metrics.parentChangeAvg || 0).toFixed(3)],
        ['Flapping avg', Number(metrics.flappingAvg || 0).toFixed(3)],
      ].map((row) => '<tr><td>' + row[0] + '</td><td>' + row[1] + '</td></tr>').join('') + '</tbody>';

      const wrapper = document.createElement('div');
      wrapper.appendChild(table);

      if (Array.isArray(rationale) && rationale.length > 0) {
        const list = document.createElement('ul');
        list.className = 'rationale-list';
        for (const item of rationale) {
          const li = document.createElement('li');
          li.textContent = String(item);
          list.appendChild(li);
        }
        wrapper.appendChild(list);
      }

      return wrapper;
    }

    function renderNetworkDetails() {
      const root = document.getElementById('network-details');

      for (const network of REPORT.networkDetails || []) {
        const details = document.createElement('details');
        details.open = false;

        const summary = document.createElement('summary');
        summary.textContent = network.label + ' | best=' + Number(network.bestAvgScore || 0).toFixed(1) + ' | seed=' + network.bestRunSeed;
        details.appendChild(summary);

        const topRow = document.createElement('div');
        topRow.className = 'network-top';

        const topologyPanel = document.createElement('div');
        topologyPanel.className = 'panel';
        const topologyCanvas = document.createElement('canvas');
        topologyCanvas.width = 520;
        topologyCanvas.height = 260;
        topologyPanel.appendChild(topologyCanvas);
        topRow.appendChild(topologyPanel);

        const paramPanel = document.createElement('div');
        paramPanel.className = 'panel';
        const heading = document.createElement('h3');
        heading.textContent = 'Best parameter vector';
        paramPanel.appendChild(heading);
        paramPanel.appendChild(renderParametersTable(network));

        const passHeading = document.createElement('h3');
        passHeading.textContent = 'PASS metrics';
        paramPanel.appendChild(passHeading);
        paramPanel.appendChild(renderPassMetricsTable(network.passMetrics));

        const scoreHeading = document.createElement('h3');
        scoreHeading.textContent = 'Score diagnostics';
        paramPanel.appendChild(scoreHeading);
        paramPanel.appendChild(
          renderScoreMetricsTable(network.scoreMetrics, network.scoreRationale),
        );

        topRow.appendChild(paramPanel);

        details.appendChild(topRow);

        const charts = document.createElement('div');
        charts.className = 'grid-4';

        const series = network.chartSeries || {};
        const trace = network.optimizationTrace || [];

        const c1 = document.createElement('canvas'); c1.width = 420; c1.height = 240;
        const c2 = document.createElement('canvas'); c2.width = 420; c2.height = 240;
        const c3 = document.createElement('canvas'); c3.width = 420; c3.height = 240;
        const c4 = document.createElement('canvas'); c4.width = 420; c4.height = 240;

        charts.appendChild(c1);
        charts.appendChild(c2);
        charts.appendChild(c3);
        charts.appendChild(c4);

        drawTopology(topologyCanvas, network.topology);

        drawLine(c1, [
          { color: '#b45f1f', values: series.duplicates || [] },
        ], 'Duplicates per round');

        drawLine(c2, [
          { color: '#1e7c4a', values: series.coveragePercent || [] },
          { color: '#1f6fb4', values: series.eligibleCount || [] },
        ], 'Coverage (%) and eligible count');

        drawLine(c3, [
          { color: '#7b3fb8', values: series.parentChanges || [] },
          { color: '#b13335', values: series.flappingNodes || [] },
        ], 'Parent changes and flapping nodes');

        drawOptimizationTrace(c4, trace);

        details.appendChild(charts);

        root.appendChild(details);
      }
    }

    setMeta();
    renderRecommendations();
    drawHeatmap();
    renderDependencies();
    renderNetworkDetails();
    bindEmbeddedDownloads();
  </script>
</body>
</html>`;
}

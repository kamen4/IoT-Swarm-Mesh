/* Purpose: Export headless simulation result as JSON and CSV files in one user action. */

import { toCsvExport } from "./csvExport.js";
import { downloadText } from "./download.js";
import { toJsonExport } from "./jsonExport.js";

/**
 * @param {{finalSnapshot:any,snapshots:any[],filePrefix:string}} payload
 */
export function exportSimulationResults(payload) {
  const timestamp = new Date().toISOString().replace(/[:.]/g, "-");
  const prefix = payload.filePrefix || "no-ui-simulation";

  const json = toJsonExport(payload.finalSnapshot);
  const csv = toCsvExport(payload.snapshots);

  downloadText(`${prefix}-${timestamp}.json`, json, "application/json");
  downloadText(`${prefix}-${timestamp}.csv`, csv, "text/csv;charset=utf-8");
}

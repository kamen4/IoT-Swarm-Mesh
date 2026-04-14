/* Purpose: Export research artifacts as a single self-contained HTML report. */

import { downloadText } from "../export/download.js";

/**
 * @param {{filePrefix:string,html:string}} payload
 */
export function exportResearchArtifacts(payload) {
  const timestamp = new Date().toISOString().replace(/[:.]/g, "-");
  const prefix = payload.filePrefix || "research-batch";

  downloadText(
    `${prefix}-${timestamp}.html`,
    payload.html,
    "text/html;charset=utf-8",
  );
}

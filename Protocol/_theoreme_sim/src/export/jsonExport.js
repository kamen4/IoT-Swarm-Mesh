/* Purpose: Convert simulation snapshot object to stable pretty JSON export format. */

/**
 * @param {any} finalSnapshot
 * @returns {string}
 */
export function toJsonExport(finalSnapshot) {
  return JSON.stringify(finalSnapshot, null, 2);
}

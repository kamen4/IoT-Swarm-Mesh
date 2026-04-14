/* Purpose: Download text content as local file from browser without external dependencies. */

/**
 * @param {string} fileName
 * @param {string} content
 * @param {string} mimeType
 */
export function downloadText(fileName, content, mimeType) {
  const blob = new Blob([content], { type: mimeType });
  const url = URL.createObjectURL(blob);

  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);

  setTimeout(() => {
    URL.revokeObjectURL(url);
  }, 0);
}

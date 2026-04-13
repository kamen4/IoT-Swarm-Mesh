/* Purpose: Tiny DOM helpers to keep UI modules compact and consistent. */

/**
 * @param {string} tag
 * @param {Record<string, any>} attrs
 * @param {Array<Node|string>} children
 * @returns {HTMLElement}
 */
export function el(tag, attrs = {}, children = []) {
  const node = document.createElement(tag);

  for (const [key, value] of Object.entries(attrs)) {
    if (key === "className") {
      node.className = value;
    } else if (key.startsWith("on") && typeof value === "function") {
      node.addEventListener(key.slice(2).toLowerCase(), value);
    } else if (value !== undefined && value !== null) {
      node.setAttribute(key, String(value));
    }
  }

  for (const child of children) {
    if (typeof child === "string") {
      node.appendChild(document.createTextNode(child));
    } else {
      node.appendChild(child);
    }
  }

  return node;
}

/**
 * @param {HTMLElement} container
 */
export function clear(container) {
  while (container.firstChild) {
    container.removeChild(container.firstChild);
  }
}

/* Purpose: Provide shared collapsible group state helpers for settings and results panels. */

import { el } from "./dom.js";

/**
 * @param {HTMLElement} container
 * @returns {Record<string, boolean>}
 */
export function captureDetailsState(container) {
  const state = {};

  if (!container) {
    return state;
  }

  container.querySelectorAll("details[data-group-id]").forEach((details) => {
    state[details.dataset.groupId] = details.open;
  });

  return state;
}

/**
 * @param {Record<string, boolean>} state
 * @param {string} id
 * @param {boolean} fallback
 * @returns {boolean}
 */
export function resolveOpenState(state, id, fallback = true) {
  if (Object.prototype.hasOwnProperty.call(state, id)) {
    return state[id];
  }
  return fallback;
}

/**
 * @param {{
 *  id:string,
 *  title:string,
 *  tooltip:string,
 *  open:boolean,
 *  contentNodes:HTMLElement[]
 * }} options
 * @returns {HTMLElement}
 */
export function createCollapsibleDetails(options) {
  const details = el("details", {
    className: "group group-collapsible",
    "data-group-id": options.id,
    title: options.tooltip,
  });

  if (options.open) {
    details.setAttribute("open", "open");
  }

  const summary = el(
    "summary",
    { className: "group-summary", title: options.tooltip },
    [options.title],
  );

  const body = el("div", { className: "group-inner" }, options.contentNodes);
  details.appendChild(summary);
  details.appendChild(body);

  return details;
}

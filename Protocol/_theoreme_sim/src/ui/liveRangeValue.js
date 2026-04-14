/* Purpose: Keep range slider value labels synchronized in real time and emit live config updates. */

/**
 * @param {number} value
 * @returns {string}
 */
function formatDeliveryProbability(value) {
  return Number(value).toFixed(2);
}

/**
 * @param {number} value
 * @returns {string}
 */
function formatSpreadFactor(value) {
  return Number(value).toFixed(2);
}

/**
 * @param {number} value
 * @returns {string}
 */
function formatThreeDecimals(value) {
  return Number(value).toFixed(3);
}

/**
 * @param {HTMLElement} container
 * @param {string} inputName
 * @param {(value:number) => void} onChange
 * @returns {() => void}
 */
export function bindLiveRangeValue(container, inputName, onChange) {
  const input = container.querySelector(`input[name='${inputName}']`);
  const output = container.querySelector(`[data-range-value='${inputName}']`);

  if (!input || !output) {
    return () => {};
  }

  const sync = () => {
    const value = Number(input.value);
    if (inputName === "deliveryProbability") {
      output.textContent = formatDeliveryProbability(value);
    } else if (inputName === "chargeSpreadFactor") {
      output.textContent = formatSpreadFactor(value);
    } else if (
      inputName === "decayPercent" ||
      inputName === "switchHysteresisRatio" ||
      inputName === "linkLearningRate"
    ) {
      output.textContent = formatSpreadFactor(value);
    } else if (inputName === "linkMemory") {
      output.textContent = formatThreeDecimals(value);
    } else {
      output.textContent = String(value);
    }

    if (typeof onChange === "function") {
      onChange(value);
    }
  };

  input.addEventListener("input", sync);
  sync();

  return () => {
    input.removeEventListener("input", sync);
  };
}

/* Purpose: Control open/close behavior for the theorem information modal dialog. */

export class InfoModal {
  /**
   * @param {HTMLButtonElement | null} openButton
   * @param {HTMLElement | null} modal
   * @param {HTMLButtonElement | null} closeButton
   */
  constructor(openButton, modal, closeButton) {
    this.openButton = openButton;
    this.modal = modal;
    this.closeButton = closeButton;
    this.previouslyFocused = null;

    this.bindEvents();
  }

  bindEvents() {
    if (!this.openButton || !this.modal || !this.closeButton) {
      return;
    }

    this.openButton.addEventListener("click", () => this.open());
    this.closeButton.addEventListener("click", () => this.close());

    this.modal
      .querySelectorAll("[data-action='close-modal']")
      .forEach((node) => {
        node.addEventListener("click", () => this.close());
      });

    document.addEventListener("keydown", (event) => {
      if (event.key === "Escape" && this.isOpen()) {
        this.close();
      }
    });
  }

  isOpen() {
    return Boolean(this.modal && !this.modal.classList.contains("hidden"));
  }

  open() {
    if (!this.modal || !this.openButton) {
      return;
    }

    this.previouslyFocused = document.activeElement;
    this.modal.classList.remove("hidden");
    document.body.classList.add("modal-open");
    this.openButton.setAttribute("aria-expanded", "true");
    this.closeButton?.focus();
  }

  close() {
    if (!this.modal || !this.openButton) {
      return;
    }

    this.modal.classList.add("hidden");
    document.body.classList.remove("modal-open");
    this.openButton.setAttribute("aria-expanded", "false");

    if (this.previouslyFocused instanceof HTMLElement) {
      this.previouslyFocused.focus();
    }
  }
}

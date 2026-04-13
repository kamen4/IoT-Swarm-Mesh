/* Purpose: Render the mesh graph, weighted edges, parent tree links, and node charge labels on canvas. */

import { RENDERING } from "../core/constants.js";
import { chargeToNodeColor } from "./colorScale.js";
import {
  computeEdgeWeight,
  computeTreeDelta,
  formatDelta,
  formatWeight,
  styleForWeight,
} from "./edgeWeightStyle.js";

export class CanvasRenderer {
  /**
   * @param {HTMLCanvasElement} canvas
   */
  constructor(canvas) {
    this.canvas = canvas;
    this.ctx = canvas.getContext("2d");
  }

  /**
   * @param {number} width
   * @param {number} height
   */
  resize(width, height) {
    const pixelRatio = window.devicePixelRatio || 1;
    this.canvas.width = Math.floor(width * pixelRatio);
    this.canvas.height = Math.floor(height * pixelRatio);
    this.canvas.style.width = `${width}px`;
    this.canvas.style.height = `${height}px`;
    this.ctx.setTransform(pixelRatio, 0, 0, pixelRatio, 0, 0);
  }

  /**
   * @param {any} state
   */
  render(state) {
    const rect = this.canvas.getBoundingClientRect();
    if (rect.width <= 0 || rect.height <= 0) {
      return;
    }

    this.resize(rect.width, rect.height);

    const ctx = this.ctx;
    ctx.clearRect(0, 0, rect.width, rect.height);

    const viewport = this.buildViewport(state, rect.width, rect.height);

    this.drawEdges(state, viewport);
    this.drawTreeEdges(state, viewport);
    this.drawNodes(state, viewport);
  }

  /**
   * @param {any} state
   * @param {number} width
   * @param {number} height
   * @returns {{positions:Map<number,{x:number,y:number}>,width:number,height:number}}
   */
  buildViewport(state, width, height) {
    let minX = Number.POSITIVE_INFINITY;
    let minY = Number.POSITIVE_INFINITY;
    let maxX = Number.NEGATIVE_INFINITY;
    let maxY = Number.NEGATIVE_INFINITY;

    for (const node of state.nodes.values()) {
      minX = Math.min(minX, node.x);
      minY = Math.min(minY, node.y);
      maxX = Math.max(maxX, node.x);
      maxY = Math.max(maxY, node.y);
    }

    if (!Number.isFinite(minX) || !Number.isFinite(minY)) {
      return { positions: new Map(), width, height };
    }

    const dataWidth = Math.max(1, maxX - minX);
    const dataHeight = Math.max(1, maxY - minY);
    const padX = Math.min(120, width * 0.18);
    const padY = Math.min(88, height * 0.16);
    const availableWidth = Math.max(16, width - padX * 2);
    const availableHeight = Math.max(16, height - padY * 2);
    const scale = Math.min(
      availableWidth / dataWidth,
      availableHeight / dataHeight,
    );

    const drawWidth = dataWidth * scale;
    const drawHeight = dataHeight * scale;
    const left = (width - drawWidth) / 2;
    const top = (height - drawHeight) / 2;

    const positions = new Map();
    for (const node of state.nodes.values()) {
      const x = left + (node.x - minX) * scale;
      const y = top + (node.y - minY) * scale;
      positions.set(node.id, { x, y });
    }

    return { positions, width, height };
  }

  /**
   * @param {string} text
   * @param {number} x
   * @param {number} y
   * @param {{width:number,height:number}} viewport
   */
  drawTextInside(text, x, y, viewport) {
    const ctx = this.ctx;
    const margin = 5;
    const textWidth = ctx.measureText(text).width;
    const clampedX = Math.min(
      Math.max(margin, x),
      viewport.width - textWidth - margin,
    );
    const clampedY = Math.min(Math.max(12, y), viewport.height - margin);
    ctx.fillText(text, clampedX, clampedY);
  }

  /**
   * @param {any} state
   */
  drawEdges(state, viewport) {
    const ctx = this.ctx;
    const { minCharge, maxCharge } = state.chargeBounds;

    for (const edge of state.edges) {
      const a = viewport.positions.get(edge.a);
      const b = viewport.positions.get(edge.b);
      const weight = computeEdgeWeight(edge, state.nodes, state.estimates);
      const style = styleForWeight(weight, minCharge, maxCharge, false);

      ctx.strokeStyle = style.stroke;
      ctx.lineWidth = style.lineWidth;
      ctx.beginPath();
      ctx.moveTo(a.x, a.y);
      ctx.lineTo(b.x, b.y);
      ctx.stroke();

      const mx = (a.x + b.x) / 2;
      const my = (a.y + b.y) / 2;
      ctx.font = RENDERING.edgeLabelFont;
      ctx.fillStyle = style.label;
      this.drawTextInside(formatWeight(weight), mx + 3, my - 2, viewport);
    }
  }

  /**
   * @param {any} state
   */
  drawTreeEdges(state, viewport) {
    const ctx = this.ctx;
    const { minCharge, maxCharge } = state.chargeBounds;

    for (const [childId, parentId] of state.parentMap.entries()) {
      if (parentId === null) {
        continue;
      }

      const child = viewport.positions.get(childId);
      const parent = viewport.positions.get(parentId);
      const delta = computeTreeDelta(childId, parentId, state.nodes);
      const style = styleForWeight(delta, minCharge, maxCharge, true);

      ctx.strokeStyle = style.stroke;
      ctx.lineWidth = style.lineWidth * RENDERING.treeEdgeBoost;
      ctx.beginPath();
      ctx.moveTo(parent.x, parent.y);
      ctx.lineTo(child.x, child.y);
      ctx.stroke();

      const t = 0.35;
      const lx = parent.x + (child.x - parent.x) * t;
      const ly = parent.y + (child.y - parent.y) * t;
      ctx.font = RENDERING.edgeLabelFont;
      ctx.fillStyle = style.label;
      this.drawTextInside(formatDelta(delta), lx + 4, ly + 12, viewport);
    }
  }

  /**
   * @param {any} state
   */
  drawNodes(state, viewport) {
    const ctx = this.ctx;
    const { minCharge, maxCharge } = state.chargeBounds;

    for (const node of state.nodes.values()) {
      const position = viewport.positions.get(node.id);
      const fill = chargeToNodeColor(
        node.qTotal,
        minCharge,
        maxCharge,
        node.isGateway,
        node.eligible,
      );

      ctx.save();
      if (!node.eligible) {
        ctx.globalAlpha = 0.48;
      }

      ctx.beginPath();
      ctx.arc(
        position.x,
        position.y,
        RENDERING.nodeRadius + (node.isGateway ? 4 : 0),
        0,
        Math.PI * 2,
      );
      ctx.fillStyle = fill;
      ctx.fill();

      ctx.lineWidth = 1.2;
      ctx.strokeStyle = "rgba(12, 55, 53, 0.88)";
      ctx.stroke();

      ctx.font = RENDERING.nodeLabelFont;
      ctx.fillStyle = "rgba(18, 37, 35, 0.92)";
      this.drawTextInside(
        `#${node.id} q=${node.qTotal.toFixed(0)}`,
        position.x + 10,
        position.y - 9,
        viewport,
      );

      if (node.parent !== null) {
        ctx.fillStyle = "rgba(25, 86, 84, 0.8)";
        this.drawTextInside(
          `p=${node.parent}`,
          position.x + 10,
          position.y + 8,
          viewport,
        );
      }

      ctx.restore();
    }
  }
}

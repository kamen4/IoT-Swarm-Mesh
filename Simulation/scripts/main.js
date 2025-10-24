"use strict";

class DeviceRenderer {
  static RADIUS = { hub: 30, lamp: 20, sensor: 15 };
  static COLORS = {
    hub: "#0078d4",
    lamp: "#ffe066",
    sensor: "#3cb371",
    selected: "#d4002a",
  };

  static draw(ctx, device, isSelected) {
    const radius = this.RADIUS[device.type];
    const color = isSelected ? this.COLORS.selected : this.COLORS[device.type];

    // Draw radius
    if (isSelected) {
      ctx.setLineDash([5, 5]);
      ctx.strokeStyle = "rgba(0, 120, 212, 0.3)";
      ctx.beginPath();
      ctx.arc(device.x, device.y, device.params.radius, 0, Math.PI * 2);
      ctx.stroke();
    }

    // Draw device
    ctx.setLineDash([]);
    ctx.fillStyle = color;
    ctx.strokeStyle = "#dde3ee";
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.arc(device.x, device.y, radius, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    // Draw name
    ctx.fillStyle = "#222";
    ctx.font = "14px Arial";
    ctx.fillText(device.params.name, device.x - radius, device.y - radius - 8);
  }
}

class DeviceManager {
  constructor() {
    this.devices = [];
    this.packetFrom = null;
    this.selected = null;
    this.dragOffset = null;
    this.editing = null;
  }

  add(type, params) {
    const radius = DeviceRenderer.RADIUS[type];
    const x = Math.random() * (canvas.width - radius * 2) + radius;
    const y = Math.random() * (canvas.height - radius * 2) + radius;
    this.devices.push(new Device(type, x, y, params));
  }

  deleteSelected() {
    if (this.selected) {
      this.devices = this.devices.filter((d) => d !== this.selected);
      this.selected = null;
    }
  }

  getAt(x, y) {
    return this.devices.find((d) => {
      const dist = Math.hypot(d.x - x, d.y - y);
      return dist < DeviceRenderer.RADIUS[d.type];
    });
  }
}

class UIManager {
  constructor(deviceManager, renderer, simulation) {
    this.dm = deviceManager;
    this.renderer = renderer;
    this.simulation = simulation;
    this.modal = $("#add-device-modal");
    this.form = $("#device-form")[0];
    this.setupEvents();
  }

  setupEvents() {
    // Canvas events
    $("#canvas").on({
      mousedown: (e) => this.handleMouseDown(e),
      mousemove: (e) => this.handleMouseMove(e),
      mouseup: () => (this.dm.dragOffset = null),
    });

    // Buttons
    $("#open-add-modal").click(() => this.openModal());
    $("#close-modal").click(() => this.closeModal());
    $("#delete-device").click(() => this.deleteDevice());
    $("#edit-device").click(() => this.editDevice());
    $("#send-packet").click(() => this.sendPacket());

    // Form
    $(this.form).on("submit", (e) => this.handleSubmit(e));

    // Window
    $(window).on("resize", () => this.resize());
  }

  handleMouseDown(e) {
    const pos = this.getMousePos(e);
    const device = this.dm.getAt(pos.x, pos.y);

    this.dm.selected = device;
    if (device) {
      this.dm.dragOffset = { x: pos.x - device.x, y: pos.y - device.y };
      this.showDeviceInfo(device);
      $("#device-selected-buttons").show();
      if (device.type == "hub") {
        $("#hub-controls").show();
      } else {
        $("#hub-controls").hide();
      }

      if (this.dm.packetFrom) {
        this.dm.packetFrom.acceptPacket(
          new Packet(null, null, Packet.REQUEST_TYPES.ping, {
            trace: [],
            originId: this.dm.packetFrom.id,
            destinationId: this.dm.selected.id,
          })
        );
        this.dm.packetFrom = null;
      }
    } else {
      $("#selected-device-info").empty();
      $("#device-selected-buttons").hide();
      $("#hub-controls").hide();
      this.dm.packetFrom = null;
    }
    render();
  }

  handleMouseMove(e) {
    if (this.dm.selected && this.dm.dragOffset) {
      const pos = this.getMousePos(e);
      this.dm.selected.x = pos.x - this.dm.dragOffset.x;
      this.dm.selected.y = pos.y - this.dm.dragOffset.y;
      render();
    }
  }

  handleSubmit(e) {
    e.preventDefault();
    const formData = new FormData(this.form);
    const params = Object.fromEntries(formData);

    // Convert number fields
    const numFields = ["battery", "radius"];
    numFields.forEach(
      (field) => (params[field] = parseFloat(params[field]) || 0)
    );

    if (this.dm.editing) {
      Object.assign(this.dm.editing.params, params);
      this.dm.editing.type = params.type;
    } else {
      this.dm.add(params.type, params);
    }

    this.closeModal();
    render();
  }

  getMousePos(e) {
    const rect = canvas.getBoundingClientRect();
    return {
      x: e.clientX - rect.left,
      y: e.clientY - rect.top,
    };
  }

  openModal() {
    this.dm.editing = null;
    this.form.reset();
    $("#modal-title").text("Add Device");
    this.modal.show();
  }

  closeModal() {
    this.modal.hide();
    this.dm.editing = null;
  }

  deleteDevice() {
    this.dm.deleteSelected();
    $("#selected-device-info").empty();
    $("#device-selected-buttons").hide();
    render();
  }

  editDevice() {
    if (!this.dm.selected) return;

    this.dm.editing = this.dm.selected;
    $("#modal-title").text("Edit Device");

    // Fill form with device data
    const { type, params } = this.dm.editing;
    const fields = {
      "device-type": type,
      "device-name": params.name,
      "device-battery": params.battery,
      "device-powerType": params.powerType,
      "device-radius": params.radius,
    };

    Object.entries(fields).forEach(([id, value]) => $(`#${id}`).val(value));
    this.modal.show();
  }

  sendPacket() {
    if (!this.dm.selected) return;
    this.dm.packetFrom = this.dm.selected;
  }

  showDeviceInfo(device) {
    const html = Object.entries({
      Type: device.type,
      ID: device.id,
      Coordinates: `(${device.x.toFixed(1)}, ${device.y.toFixed(1)})`,
      ...device.params,
      Battery: `${(device.params.battery * 100).toFixed(0)}%`,
    })
      .map(([k, v]) => `<div><strong>${k}:</strong> ${v}</div>`)
      .join("");

    $("#selected-device-info").html(html);
  }

  resize() {
    const $main = $("main");
    const $panel = $("#control-panel");
    canvas.width = $main.width() - $panel.outerWidth(true);
    canvas.height = $main.height();
    $("#canvas").css({ width: canvas.width, height: canvas.height });
    render();
  }
}

// Global elements
const canvas = $("#canvas")[0];
const ctx = canvas.getContext("2d");

// Initialize managers
const deviceManager = new DeviceManager();
Packet.dm = deviceManager;
const simulation = new Simulation(deviceManager, DeviceRenderer);
const uiManager = new UIManager(deviceManager, DeviceRenderer, simulation);

// Render function
function render() {
  ctx.clearRect(0, 0, canvas.width, canvas.height);
  deviceManager.devices.forEach((device) =>
    DeviceRenderer.draw(ctx, device, device === deviceManager.selected)
  );

  simulation.updateConnections();
  SimulationRenderer.drawConnections(ctx, simulation);

  SimulationRenderer.drawPackets(ctx, simulation);
}

// Initial setup
$(document).ready(() => {
  uiManager.resize();
  uiManager.closeModal();

  deviceManager.add("hub", { radius: 400 });
  deviceManager.add("sensor", { radius: 400 });
  deviceManager.add("sensor", { radius: 400 });
  deviceManager.add("sensor", { radius: 400 });
  deviceManager.add("sensor", { radius: 400 });
  deviceManager.add("sensor", { radius: 400 });

  setInterval(() => render(), 10);
});

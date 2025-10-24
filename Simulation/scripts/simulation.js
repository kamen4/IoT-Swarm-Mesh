class Connection {
  constructor(d1, d2, weight) {
    this.d1 = d1;
    this.d2 = d2;
    this.weight = weight;
  }
}

class Simulation {
  constructor(deviceManager, deviceRenderer) {
    this.deviceManager = deviceManager;
    this.deviceRenderer = deviceRenderer;
    this.connections = [];
  }

  updateConnections() {
    this.connections = [];
    this.deviceManager.devices.forEach((d1) => {
      this.deviceManager.devices.forEach((d2) => {
        if (d1 === d2) return;
        const maxDist = Math.min(d1.params.radius, d2.params.radius);
        const dist = Math.sqrt(
          (d1.x - d2.x) * (d1.x - d2.x) + (d1.y - d2.y) * (d1.y - d2.y)
        );
        if (maxDist >= dist) {
          if (
            !this.connections.find(
              (c) => c.d1.id === d2.id && c.d2.id === d1.id
            )
          ) {
            this.connections.push(new Connection(d1, d2, 0.25));
          }
        }
      });
    });
    this.connections.forEach((c) => {
      c.d1.connections = [];
      c.d2.connections = [];
    });
    this.connections.forEach((c) => {
      c.d1.connections.push(c);
      c.d2.connections.push(c);
    });
  }
}

class SimulationRenderer {
  static COLORS = {
    connection: "rgba(141, 32, 32, 0.48)",
  };

  static drawConnections(ctx, simulation) {
    simulation.connections.forEach((c) => {
      ctx.strokeStyle = this.COLORS.connection;
      ctx.lineWidth = 4 * c.weight;
      ctx.setLineDash([]);
      ctx.beginPath();
      ctx.moveTo(c.d1.x, c.d1.y);
      ctx.lineTo(c.d2.x, c.d2.y);
      ctx.stroke();
    });
  }

  static drawPackets(ctx, simulation) {
    const now = Date.now();
    Packet.packets.forEach((p) => {
      const progress = (now - p.created) / p.estDeliveryTime;
      if (progress >= 1) {
        if (p.to) {
          p.to.acceptPacket(p);
        }
        p.dispose();
        return;
      }
      if (!p || !p.to || !p.from) {
        return;
      }
      // Lerp
      const x = p.from.x + (p.to.x - p.from.x) * progress;
      const y = p.from.y + (p.to.y - p.from.y) * progress;
      ctx.fillStyle = "#0078d4";
      ctx.fillRect(x - 4, y - 4, 8, 8);
    });
  }
}

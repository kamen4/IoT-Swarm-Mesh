class Packet {
  static dm;

  static REQUEST_TYPES = {
    buildTree: "build-tree",
    buildTreeBack: "build-tree-back",
    ping: "ping",
    pingBack: "ping-back",
  };

  static packets = [];

  constructor(from, to, type, data, ttl = 16) {
    this.id = "p_" + Date.now() * 1000 + Math.round(Math.random() * 1000);
    this.created = Date.now();
    if (to && from) {
      this.estDeliveryTime = Math.round(to.distanceTo(from) / 0.2);
    } else {
      this.estDeliveryTime = 1000;
    }
    this.ttl = ttl;
    this.from = from;
    this.to = to;
    this.type = type;
    this.data = data;

    Packet.packets.push(this);
  }

  dispose() {
    const index = Packet.packets.indexOf(this);
    if (index > -1) {
      Packet.packets.splice(index, 1);
    }
  }
}

class Device {
  constructor(type, x, y, params = {}) {
    this.id = "d_" + Date.now() * 1000 + Math.round(Math.random() * 1000);
    this.type = type;
    this.x = x;
    this.y = y;
    this.params = {
      name: type.charAt(0).toUpperCase() + type.slice(1),
      battery: 1,
      powerType: "Battery",
      radius: 100,
      ...params,
    };

    this.connections = [];
  }

  distanceTo(otherDevice) {
    return Math.sqrt(
      (this.x - otherDevice.x) * (this.x - otherDevice.x) +
        (this.y - otherDevice.y) * (this.y - otherDevice.y)
    );
  }

  acceptPacket(packet) {
    console.log(packet);
    if (packet.type === Packet.REQUEST_TYPES.ping) {
      if (packet.data.destinationId === this.id) {
        new Packet(
          this,
          packet.data.trace.at(-1),
          Packet.REQUEST_TYPES.pingBack,
          {
            trace: packet.data.trace.slice(0, -1),
            originId: this.id,
            destinationId: packet.data.originId,
          }
        );
        return;
      }
      if (packet.ttl > 0) {
        this.connections
          .map((c) => (c.d1.id === this.id ? c.d2 : c.d1))
          .forEach((n) => {
            if (packet.data.trace.find((d) => d.id === n.id)) {
              return;
            }
            new Packet(
              this,
              n,
              Packet.REQUEST_TYPES.ping,
              {
                trace: [...packet.data.trace, this],
                originId: packet.data.originId,
                destinationId: packet.data.destinationId,
              },
              packet.ttl - 1
            );
          });
      }
      return;
    }
    if (packet.type === Packet.REQUEST_TYPES.pingBack) {
      if (packet.data.destinationId === this.id) {
        alert("packet sent");
        return;
      }
      new Packet(
        this,
        packet.data.trace.at(-1),
        Packet.REQUEST_TYPES.pingBack,
        {
          trace: packet.data.trace.slice(0, -1),
          originId: packet.data.originId,
          destinationId: packet.data.destinationId,
        },
        packet.ttl - 1
      );
    }
  }
}

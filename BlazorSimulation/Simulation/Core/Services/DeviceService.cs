using Core.Devices;
using System.Numerics;

namespace Core.Services;

public class DeviceService : IDeviceService
{
    private readonly HashSet<Device> _devices = [];
    private readonly Dictionary<string, Func<List<Device>>> _presets;

    public DeviceService()
    {
        _presets = new Dictionary<string, Func<List<Device>>>
        {
            ["Diamonds"] = CreateDiamondsPreset,
            ["Line"] = CreateLinePreset,
            ["Star"] = CreateStarPreset,
            ["Grid"] = CreateGridPreset,
            ["Sparse"] = CreateSparsePreset,
            ["Tree"] = CreateTreePreset,
            ["Dense"] = CreateDensePreset,
            ["Chain"] = CreateChainPreset
        };
    }

    public IReadOnlyCollection<Device> All => _devices;
    
    public Hub? Hub => _devices.OfType<Hub>().FirstOrDefault();
    
    public IEnumerable<Lamp> Lamps => _devices.OfType<Lamp>();
    
    public IEnumerable<Sensor> Sensors => _devices.OfType<Sensor>();

    public event Action? OnDevicesChanged;

    public void Add(Device device)
    {
        _devices.Add(device);
        OnDevicesChanged?.Invoke();
    }

    public void Remove(Device device)
    {
        // Remove from all connections
        foreach (var d in _devices)
        {
            d.Connections.Remove(device);
        }
        _devices.Remove(device);
        OnDevicesChanged?.Invoke();
    }

    public void Clear()
    {
        _devices.Clear();
        OnDevicesChanged?.Invoke();
    }

    public Device? GetById(Guid id) => _devices.FirstOrDefault(d => d.Id == id);

    public Device? GetAtPosition(Vector2 pos, float tolerance = 15f)
    {
        return _devices
            .Reverse()
            .FirstOrDefault(d =>
            {
                var posDif = d.Pos - pos;
                var distSquared = tolerance * tolerance;
                return posDif.LengthSquared() < distSquared;
            });
    }

    public IEnumerable<Device> GetVisibleDevices(Device device)
    {
        foreach (var d in _devices)
        {
            if (d != device && AreDevicesVisible(d, device))
            {
                yield return d;
            }
        }
    }

    public IEnumerable<(Device d1, Device d2)> GetAllVisibilities()
    {
        var deviceList = _devices.ToList();
        for (int i = 0; i < deviceList.Count; i++)
        {
            for (int j = i + 1; j < deviceList.Count; j++)
            {
                if (AreDevicesVisible(deviceList[i], deviceList[j]))
                {
                    yield return (deviceList[i], deviceList[j]);
                }
            }
        }
    }

    public bool AreDevicesVisible(Device d1, Device d2)
    {
        var distance = Vector2.Distance(d1.Pos, d2.Pos);
        var minRadius = Math.Min(d1.Radius, d2.Radius);
        return distance <= minRadius;
    }

    public void LoadPreset(string presetName)
    {
        if (_presets.TryGetValue(presetName, out var factory))
        {
            Clear();
            foreach (var device in factory())
            {
                _devices.Add(device);
            }
            OnDevicesChanged?.Invoke();
        }
    }

    public IEnumerable<string> GetPresetNames() => _presets.Keys;

    // Пресеты
    private static List<Device> CreateDiamondsPreset()
    {
        return
        [
            new Hub { Name = "H", Pos = new(50, 50), Radius = 200 },
            new Lamp { Name = "L1", Pos = new(50, 250), Radius = 300 },
            new Sensor { Name = "S1", Pos = new(50, 450), Radius = 300 },
            new Lamp { Name = "L2", Pos = new(250, 450), Radius = 300 },
            new Sensor { Name = "S2", Pos = new(250, 250), Radius = 300 },
            new Sensor { Name = "S3", Pos = new(550, 450), Radius = 300 },
            new Lamp { Name = "L3", Pos = new(750, 450), Radius = 300 },
            new Lamp { Name = "L4", Pos = new(550, 650), Radius = 300 },
            new Sensor { Name = "S4", Pos = new(750, 650), Radius = 300 },
            new Sensor { Name = "S5", Pos = new(1050, 650), Radius = 300 }
        ];
    }

    private static List<Device> CreateLinePreset()
    {
        return
        [
            new Hub { Name = "H", Pos = new(100, 300), Radius = 150 },
            new Lamp { Name = "L1", Pos = new(250, 300), Radius = 150 },
            new Sensor { Name = "S1", Pos = new(400, 300), Radius = 150 },
            new Lamp { Name = "L2", Pos = new(550, 300), Radius = 150 },
            new Sensor { Name = "S2", Pos = new(700, 300), Radius = 150 }
        ];
    }

    private static List<Device> CreateStarPreset()
    {
        var devices = new List<Device>
        {
            new Hub { Name = "H", Pos = new(400, 300), Radius = 250 }
        };
        
        for (int i = 0; i < 6; i++)
        {
            var angle = i * Math.PI * 2 / 6;
            var x = 400 + (float)(Math.Cos(angle) * 200);
            var y = 300 + (float)(Math.Sin(angle) * 200);
            
            if (i % 2 == 0)
                devices.Add(new Lamp { Name = $"L{i / 2 + 1}", Pos = new(x, y), Radius = 150 });
            else
                devices.Add(new Sensor { Name = $"S{i / 2 + 1}", Pos = new(x, y), Radius = 150 });
        }
        
        return devices;
    }

    private static List<Device> CreateGridPreset()
    {
        var devices = new List<Device>
        {
            new Hub { Name = "H", Pos = new(100, 100), Radius = 200 }
        };
        
        int idx = 1;
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                if (row == 0 && col == 0) continue;
                
                var x = 100 + col * 150;
                var y = 100 + row * 150;
                
                if ((row + col) % 2 == 0)
                    devices.Add(new Lamp { Name = $"L{idx++}", Pos = new(x, y), Radius = 180 });
                else
                    devices.Add(new Sensor { Name = $"S{idx++}", Pos = new(x, y), Radius = 180 });
            }
        }
        
        return devices;
    }

    private static List<Device> CreateSparsePreset()
    {
        return
        [
            new Hub { Name = "H", Pos = new(100, 100), Radius = 200 },
            new Lamp { Name = "L1", Pos = new(250, 150), Radius = 200 },
            new Sensor { Name = "S1", Pos = new(450, 100), Radius = 200 },
            new Lamp { Name = "L2", Pos = new(650, 200), Radius = 200 },
            new Sensor { Name = "S2", Pos = new(200, 350), Radius = 200 },
            new Lamp { Name = "L3", Pos = new(400, 400), Radius = 200 },
            new Sensor { Name = "S3", Pos = new(600, 450), Radius = 200 },
            new Lamp { Name = "L4", Pos = new(800, 350), Radius = 200 }
        ];
    }

    private static List<Device> CreateTreePreset()
    {
        return
        [
            new Hub { Name = "H", Pos = new(400, 50), Radius = 180 },
            new Lamp { Name = "L1", Pos = new(250, 150), Radius = 180 },
            new Lamp { Name = "L2", Pos = new(550, 150), Radius = 180 },
            new Sensor { Name = "S1", Pos = new(150, 280), Radius = 180 },
            new Sensor { Name = "S2", Pos = new(350, 280), Radius = 180 },
            new Sensor { Name = "S3", Pos = new(450, 280), Radius = 180 },
            new Sensor { Name = "S4", Pos = new(650, 280), Radius = 180 },
            new Lamp { Name = "L3", Pos = new(100, 400), Radius = 180 },
            new Lamp { Name = "L4", Pos = new(200, 400), Radius = 180 },
            new Lamp { Name = "L5", Pos = new(600, 400), Radius = 180 },
            new Lamp { Name = "L6", Pos = new(700, 400), Radius = 180 }
        ];
    }

    private static List<Device> CreateDensePreset()
    {
        var devices = new List<Device>
        {
            new Hub { Name = "H", Pos = new(300, 250), Radius = 200 }
        };
        
        var random = new Random(42);
        for (int i = 0; i < 15; i++)
        {
            var x = 100 + random.Next(400);
            var y = 100 + random.Next(300);
            
            if (i % 2 == 0)
                devices.Add(new Lamp { Name = $"L{i / 2 + 1}", Pos = new(x, y), Radius = 150 });
            else
                devices.Add(new Sensor { Name = $"S{i / 2 + 1}", Pos = new(x, y), Radius = 150 });
        }
        
        return devices;
    }

    private static List<Device> CreateChainPreset()
    {
        var devices = new List<Device>
        {
            new Hub { Name = "H", Pos = new(50, 200), Radius = 120 }
        };
        
        for (int i = 0; i < 10; i++)
        {
            var x = 150 + i * 80;
            var y = 200 + (float)Math.Sin(i * 0.5) * 50;
            
            if (i % 2 == 0)
                devices.Add(new Lamp { Name = $"L{i / 2 + 1}", Pos = new(x, y), Radius = 100 });
            else
                devices.Add(new Sensor { Name = $"S{i / 2 + 1}", Pos = new(x, y), Radius = 100 });
        }
        
        return devices;
    }
}

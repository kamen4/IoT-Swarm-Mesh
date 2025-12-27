using Core;
using Core.Devices;
using Core.Statistics;

namespace Tests;

public class StatisticsServiceTests
{
    private readonly IStatisticsService _statistics;

    public StatisticsServiceTests()
    {
        _statistics = new StatisticsService();
    }

    [Fact]
    public void RecordPacketCreated_IncrementsCount()
    {
        var packet = new Packet(new Hub(), new Lamp());
        
        _statistics.RecordPacketCreated(packet);
        
        Assert.Equal(1, _statistics.Current.TotalPacketsCreated);
    }

    [Fact]
    public void RecordPacketDelivered_IncrementsCountAndUpdatesStats()
    {
        var packet = new Packet(new Hub(), new Lamp());
        
        _statistics.RecordPacketCreated(packet);
        _statistics.RecordPacketDelivered(packet, TimeSpan.FromMilliseconds(100));
        
        Assert.Equal(1, _statistics.Current.TotalPacketsDelivered);
        Assert.Equal(100, _statistics.Current.AverageDeliveryTimeMs);
    }

    [Fact]
    public void RecordPacketDelivered_TracksMinMaxTimes()
    {
        var packet1 = new Packet(new Hub(), new Lamp());
        var packet2 = new Packet(new Hub(), new Lamp());
        
        _statistics.RecordPacketCreated(packet1);
        _statistics.RecordPacketCreated(packet2);
        _statistics.RecordPacketDelivered(packet1, TimeSpan.FromMilliseconds(50));
        _statistics.RecordPacketDelivered(packet2, TimeSpan.FromMilliseconds(150));
        
        Assert.Equal(50, _statistics.Current.MinDeliveryTimeMs);
        Assert.Equal(150, _statistics.Current.MaxDeliveryTimeMs);
    }

    [Fact]
    public void RecordPacketDropped_IncrementsCount()
    {
        var packet = new Packet(new Hub(), new Lamp());
        
        _statistics.RecordPacketCreated(packet);
        _statistics.RecordPacketDropped(packet, "Test reason");
        
        Assert.Equal(1, _statistics.Current.TotalPacketsDropped);
    }

    [Fact]
    public void RecordPacketForwarded_IncrementsCount()
    {
        var packet = new Packet(new Hub(), new Lamp());
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        
        _statistics.RecordPacketForwarded(packet, fromId, toId, 100f);
        
        Assert.Equal(1, _statistics.Current.TotalPacketsForwarded);
    }

    [Fact]
    public void RecordPacketLost_IncrementsCount()
    {
        var packet = new Packet(new Hub(), new Lamp());
        
        _statistics.RecordPacketLost(packet, 100f, 0.1);
        
        Assert.Equal(1, _statistics.Current.TotalPacketsLost);
    }

    [Fact]
    public void RecordHop_IncrementsCount()
    {
        var packet = new Packet(new Hub(), new Lamp());
        
        _statistics.RecordHop(packet, Guid.NewGuid());
        
        Assert.Equal(1, _statistics.Current.TotalHops);
    }

    [Fact]
    public void RecordBatteryDrain_TracksPerDevice()
    {
        var deviceId = Guid.NewGuid();
        
        _statistics.RecordBatteryDrain(deviceId, 0.1);
        _statistics.RecordBatteryDrain(deviceId, 0.2);
        
        Assert.Equal(0.3, _statistics.Current.BatteryDrainByDevice[deviceId], 0.001);
    }

    [Fact]
    public void DeliveryRate_CalculatesCorrectly()
    {
        var hub = new Hub();
        var lamp = new Lamp();
        
        _statistics.RecordPacketCreated(new Packet(hub, lamp));
        _statistics.RecordPacketCreated(new Packet(hub, lamp));
        var delivered = new Packet(hub, lamp);
        _statistics.RecordPacketCreated(delivered);
        _statistics.RecordPacketDelivered(delivered, TimeSpan.FromMilliseconds(50));
        
        Assert.Equal(100.0 / 3.0, _statistics.Current.DeliveryRate, 0.1);
    }

    [Fact]
    public void Reset_ClearsAllStats()
    {
        var packet = new Packet(new Hub(), new Lamp());
        _statistics.RecordPacketCreated(packet);
        _statistics.RecordPacketDelivered(packet, TimeSpan.FromMilliseconds(100));
        
        _statistics.Reset();
        
        Assert.Equal(0, _statistics.Current.TotalPacketsCreated);
        Assert.Equal(0, _statistics.Current.TotalPacketsDelivered);
    }

    [Fact]
    public void ExportToCsv_ContainsAllMetrics()
    {
        var packet = new Packet(new Hub(), new Lamp());
        _statistics.RecordPacketCreated(packet);
        
        var csv = _statistics.ExportToCsv();
        
        Assert.Contains("TotalPacketsCreated", csv);
        Assert.Contains("DeliveryRate", csv);
        Assert.Contains("LossRate", csv);
    }

    [Fact]
    public void ExportPacketLogToCsv_ContainsHeaders()
    {
        var csv = _statistics.ExportPacketLogToCsv();
        
        Assert.Contains("PacketId", csv);
        Assert.Contains("SenderName", csv);
        Assert.Contains("HopCount", csv);
    }

    [Fact]
    public void ExportHopLogToCsv_ContainsHeaders()
    {
        var csv = _statistics.ExportHopLogToCsv();
        
        Assert.Contains("HopNumber", csv);
        Assert.Contains("Distance", csv);
    }
}

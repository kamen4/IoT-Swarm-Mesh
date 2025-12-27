using Core.Protocol;
using Core.Protocol.Routing;

namespace Tests;

public class RoutingProtocolTests
{
    private class MockNetworkNode : INetworkNode
    {
        public Guid NodeId { get; }
        private readonly Dictionary<Guid, MockNetworkNode> _neighbors = [];
        private readonly List<(Guid, byte[])> _sentMessages = [];

        public IReadOnlyList<(Guid TargetId, byte[] Data)> SentMessages => _sentMessages;

        public MockNetworkNode(Guid? id = null)
        {
            NodeId = id ?? Guid.NewGuid();
        }

        public void AddNeighbor(MockNetworkNode neighbor)
        {
            _neighbors[neighbor.NodeId] = neighbor;
            neighbor._neighbors[NodeId] = this;
        }

        public void Send(Guid neighborId, byte[] data)
        {
            _sentMessages.Add((neighborId, data));
        }

        public void OnReceive(Guid fromNodeId, byte[] data)
        {
            // Handled by protocol
        }

        public IEnumerable<Guid> GetNeighbors() => _neighbors.Keys;

        public bool IsNeighborReachable(Guid neighborId) => _neighbors.ContainsKey(neighborId);

        public void ClearSentMessages() => _sentMessages.Clear();
    }

    [Fact]
    public void FloodingProtocol_Broadcast_SendsToAllNeighbors()
    {
        var node1 = new MockNetworkNode();
        var node2 = new MockNetworkNode();
        var node3 = new MockNetworkNode();
        
        node1.AddNeighbor(node2);
        node1.AddNeighbor(node3);

        var protocol = new FloodingProtocol();
        protocol.Initialize(node1);

        var message = new PingMessage();
        protocol.Broadcast(message);

        Assert.Equal(2, node1.SentMessages.Count);
        Assert.Contains(node1.SentMessages, m => m.TargetId == node2.NodeId);
        Assert.Contains(node1.SentMessages, m => m.TargetId == node3.NodeId);
    }

    [Fact]
    public void FloodingProtocol_HandleMessage_ForwardsToNeighbors()
    {
        var node1 = new MockNetworkNode();
        var node2 = new MockNetworkNode();
        var node3 = new MockNetworkNode();
        
        node2.AddNeighbor(node1);
        node2.AddNeighbor(node3);

        var protocol = new FloodingProtocol();
        protocol.Initialize(node2);

        var message = new PingMessage { TTL = 64 };
        protocol.HandleMessage(message, node1.NodeId);

        // Should forward to node3 but not back to node1
        Assert.Single(node2.SentMessages);
        Assert.Equal(node3.NodeId, node2.SentMessages[0].TargetId);
    }

    [Fact]
    public void FloodingProtocol_HandleMessage_IgnoresDuplicates()
    {
        var node1 = new MockNetworkNode();
        var node2 = new MockNetworkNode();
        
        node1.AddNeighbor(node2);

        var protocol = new FloodingProtocol();
        protocol.Initialize(node1);

        var message = new PingMessage { TTL = 64 };
        protocol.HandleMessage(message, node2.NodeId);
        protocol.HandleMessage(message, node2.NodeId); // Same message

        // Should only process once
        Assert.Empty(node1.SentMessages); // No neighbors except sender
    }

    [Fact]
    public void FloodingProtocol_HandleMessage_DecrementsTTL()
    {
        var node1 = new MockNetworkNode();
        var node2 = new MockNetworkNode();
        var node3 = new MockNetworkNode();
        
        node2.AddNeighbor(node1);
        node2.AddNeighbor(node3);

        var protocol = new FloodingProtocol();
        protocol.Initialize(node2);

        var message = new PingMessage { TTL = 1 };
        protocol.HandleMessage(message, node1.NodeId);

        // TTL was 1, after decrement it's 0, but message was already forwarded
        Assert.Single(node2.SentMessages);
        
        var forwardedData = node2.SentMessages[0].Data;
        var forwardedMessage = MessageSerializer.Deserialize(forwardedData);
        Assert.NotNull(forwardedMessage);
        Assert.Equal(0, forwardedMessage.TTL);
    }

    [Fact]
    public void FloodingProtocol_HandleMessage_StopsAtTTLZero()
    {
        var node1 = new MockNetworkNode();
        var node2 = new MockNetworkNode();
        var node3 = new MockNetworkNode();
        
        node2.AddNeighbor(node1);
        node2.AddNeighbor(node3);

        var protocol = new FloodingProtocol();
        protocol.Initialize(node2);

        var message = new PingMessage { TTL = 0 };
        protocol.HandleMessage(message, node1.NodeId);

        Assert.Empty(node2.SentMessages);
    }

    [Fact]
    public void FloodingProtocol_OnMessageReceived_Fires()
    {
        var node = new MockNetworkNode();
        var protocol = new FloodingProtocol();
        protocol.Initialize(node);

        ProtocolMessage? received = null;
        protocol.OnMessageReceived += m => received = m;

        var message = new PingMessage { ReceiverId = node.NodeId };
        protocol.HandleMessage(message, Guid.NewGuid());

        Assert.NotNull(received);
        Assert.Equal(message.MessageId, received.MessageId);
    }

    [Fact]
    public void TreeRoutingProtocol_SendTo_UsesParentForUnknownTarget()
    {
        var parentId = Guid.NewGuid();
        var node = new MockNetworkNode();
        
        // Simulate parent as neighbor
        var parent = new MockNetworkNode(parentId);
        node.AddNeighbor(parent);

        var protocol = new TreeRoutingProtocol();
        protocol.Initialize(node);
        protocol.SetParent(parentId);

        var message = new PingMessage();
        protocol.SendTo(Guid.NewGuid(), message);

        Assert.Single(node.SentMessages);
        Assert.Equal(parentId, node.SentMessages[0].TargetId);
    }

    [Fact]
    public void TreeRoutingProtocol_SendTo_UsesChildDirectly()
    {
        var childId = Guid.NewGuid();
        var node = new MockNetworkNode();
        
        var child = new MockNetworkNode(childId);
        node.AddNeighbor(child);

        var protocol = new TreeRoutingProtocol();
        protocol.Initialize(node);
        protocol.AddChild(childId);

        var message = new PingMessage();
        protocol.SendTo(childId, message);

        Assert.Single(node.SentMessages);
        Assert.Equal(childId, node.SentMessages[0].TargetId);
    }

    [Fact]
    public void TreeRoutingProtocol_Broadcast_SendsToParentAndChildren()
    {
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var node = new MockNetworkNode();
        
        var parent = new MockNetworkNode(parentId);
        var child = new MockNetworkNode(childId);
        node.AddNeighbor(parent);
        node.AddNeighbor(child);

        var protocol = new TreeRoutingProtocol();
        protocol.Initialize(node);
        protocol.SetParent(parentId);
        protocol.AddChild(childId);

        var message = new PingMessage();
        protocol.Broadcast(message);

        Assert.Equal(2, node.SentMessages.Count);
        Assert.Contains(node.SentMessages, m => m.TargetId == parentId);
        Assert.Contains(node.SentMessages, m => m.TargetId == childId);
    }
}

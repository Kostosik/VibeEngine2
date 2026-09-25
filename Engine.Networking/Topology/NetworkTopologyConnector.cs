using Engine.Networking.Connections;
using Engine.Networking.Sessions;

namespace Engine.Networking.Topology;

public sealed class NetworkTopologyConnector
{
    private readonly NetworkSession _session;

    private readonly NetworkNode _localNode;

    private readonly Dictionary<
        NetworkNodeId,
        NetworkNode> _nodes;

    private readonly Dictionary<
        NetworkNodeId,
        NetworkConnection> _connections =
        new();

    public NetworkTopologyConnector(
        NetworkSession session,
        NetworkNode localNode,
        IReadOnlyList<NetworkNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(nodes);

        ValidateNodes(
            nodes,
            localNode.Id);

        _session =
            session;

        _localNode =
            localNode;

        _nodes =
            nodes.ToDictionary(
                node => node.Id);
    }

    public NetworkNode LocalNode =>
        _localNode;

    public IReadOnlyDictionary<
        NetworkNodeId,
        NetworkConnection> Connections =>
        _connections;

    public void Apply(
        NetworkTopologyPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        foreach (var peerId in
                 plan.GetInitiatedPeers(
                     _localNode.Id))
        {
            if (!_nodes.TryGetValue(
                    peerId,
                    out var peer))
            {
                throw new InvalidOperationException(
                    $"Topology references unknown node '{peerId.Value}'.");
            }

            if (_connections.TryGetValue(
                    peerId,
                    out var existing) &&
                ContainsConnection(
                    existing.Id))
            {
                continue;
            }

            var connection =
                FindExistingConnection(
                    peer);

            if (connection is null)
            {
                connection =
                    _session.Connect(
                        peer.Endpoint);
            }

            _connections[peerId] =
                connection;
        }
    }

    private bool ContainsConnection(
        ConnectionId connection)
    {
        foreach (var existing in
                 _session.Connections)
        {
            if (existing.Id == connection)
            {
                return true;
            }
        }

        return false;
    }

    private NetworkConnection? FindExistingConnection(
        NetworkNode peer)
    {
        foreach (var connection in
                 _session.Connections)
        {
            if (connection.Endpoint ==
                peer.Endpoint)
            {
                return connection;
            }
        }

        return null;
    }

    private static void ValidateNodes(
        IReadOnlyList<NetworkNode> nodes,
        NetworkNodeId localNodeId)
    {
        var ids =
            new HashSet<NetworkNodeId>();

        var localNodeFound =
            false;

        foreach (var node in nodes)
        {
            if (!node.Id.IsValid)
            {
                throw new ArgumentException(
                    "Topology contains an invalid node ID.",
                    nameof(nodes));
            }

            if (!ids.Add(node.Id))
            {
                throw new ArgumentException(
                    $"Topology contains duplicate node ID '{node.Id.Value}'.",
                    nameof(nodes));
            }

            if (node.Id == localNodeId)
            {
                localNodeFound = true;
            }
        }

        if (!localNodeFound)
        {
            throw new ArgumentException(
                $"Local node '{localNodeId.Value}' does not exist in the node list.",
                nameof(nodes));
        }
    }
}
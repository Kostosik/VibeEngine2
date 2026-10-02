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

    private NetworkTopologyPlan? _plan;

    public NetworkTopologyConnector(
        NetworkSession session,
        NetworkNode localNode,
        IReadOnlyList<NetworkNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(
            session);

        ArgumentNullException.ThrowIfNull(
            nodes);

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
        new Dictionary<
            NetworkNodeId,
            NetworkConnection>(
            _connections);

    public void Apply(
    NetworkTopologyPlan plan)
    {
        ArgumentNullException.ThrowIfNull(
            plan);

        _plan =
            plan;

        var desiredPeers =
            plan.GetPeers(
                    _localNode.Id)
                .ToHashSet();

        RemoveUndesiredConnections(
            desiredPeers);

        ReconcileActiveConnections(
            desiredPeers);

        TryConnectInitiatedPeers();
    }

    public void Update()
    {
        if (_plan is null)
        {
            return;
        }

        var desiredPeers =
            _plan.GetPeers(
                    _localNode.Id)
                .ToHashSet();

        ReconcileActiveConnections(
            desiredPeers);

        TryConnectInitiatedPeers();
    }

    private void TryConnectInitiatedPeers()
    {
        if (_plan is null)
        {
            return;
        }

        foreach (var peerId in
                 _plan.GetInitiatedPeers(
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
                    out var existing))
            {
                if (ContainsConnection(
                        existing.Id))
                {
                    continue;
                }

                _connections.Remove(
                    peerId);
            }

            var connection =
                FindExistingConnection(
                    peer);

            if (connection is not null)
            {
                _connections[peerId] =
                    connection;

                continue;
            }

            try
            {
                connection =
                    _session.Connect(
                        peer.Endpoint);
            }
            catch (InvalidOperationException)
            {
                // The remote node may not be available yet.
                // The next Update() will retry.
                continue;
            }

            _connections[peerId] =
                connection;
        }
    }

    private void ReconcileActiveConnections(
        HashSet<NetworkNodeId> desiredPeers)
    {
        var activeConnectionIds =
            new HashSet<ConnectionId>();

        foreach (var connection in
                 _session.Connections.ToArray())
        {
            var peerId =
                FindNodeId(
                    connection.Endpoint);

            if (!peerId.HasValue ||
                !desiredPeers.Contains(
                    peerId.Value))
            {
                _session.Disconnect(
                    connection.Id);

                continue;
            }

            if (_connections.TryGetValue(
                    peerId.Value,
                    out var existing))
            {
                if (existing.Id !=
                    connection.Id)
                {
                    _session.Disconnect(
                        connection.Id);

                    continue;
                }
            }
            else
            {
                _connections[peerId.Value] =
                    connection;
            }

            activeConnectionIds.Add(
                connection.Id);
        }

        var trackedPeers =
            _connections.Keys.ToArray();

        foreach (var peerId in
                 trackedPeers)
        {
            var connection =
                _connections[peerId];

            if (!activeConnectionIds.Contains(
                    connection.Id))
            {
                _connections.Remove(
                    peerId);
            }
        }
    }

    private NetworkNodeId? FindNodeId(
        NetworkEndpoint endpoint)
    {
        foreach (var node in
                 _nodes.Values)
        {
            if (node.Endpoint ==
                endpoint)
            {
                return node.Id;
            }
        }

        return null;
    }

    private void RemoveUndesiredConnections(
        HashSet<NetworkNodeId> desiredPeers)
    {
        var trackedPeers =
            _connections.Keys.ToArray();

        foreach (var peerId in trackedPeers)
        {
            if (desiredPeers.Contains(
                    peerId))
            {
                continue;
            }

            if (_connections.Remove(
                    peerId,
                    out var connection) &&
                ContainsConnection(
                    connection.Id))
            {
                _session.Disconnect(
                    connection.Id);
            }
        }
    }

    private bool ContainsConnection(
        ConnectionId connection)
    {
        foreach (var existing in
                 _session.Connections)
        {
            if (existing.Id ==
                connection)
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
                    "Network node ID must be valid.",
                    nameof(nodes));
            }

            if (!ids.Add(
                    node.Id))
            {
                throw new ArgumentException(
                    $"Network node ID '{node.Id.Value}' appears more than once.",
                    nameof(nodes));
            }

            if (node.Id == localNodeId)
            {
                localNodeFound =
                    true;
            }
        }

        if (!localNodeFound)
        {
            throw new ArgumentException(
                $"Local node '{localNodeId.Value}' does not exist in the node list.",
                nameof(localNodeId));
        }
    }
}
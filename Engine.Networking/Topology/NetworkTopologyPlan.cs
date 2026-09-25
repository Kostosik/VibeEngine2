namespace Engine.Networking.Topology;

public sealed class NetworkTopologyPlan
{
    private readonly NetworkTopologyEdge[] _edges;

    private readonly Dictionary<
        NetworkNodeId,
        NetworkNodeId[]> _peers;

    private readonly Dictionary<
        NetworkNodeId,
        NetworkNodeId[]> _initiatedPeers;

    internal NetworkTopologyPlan(
        IReadOnlyList<NetworkTopologyEdge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);

        _edges =
            edges.ToArray();

        var peers =
            new Dictionary<
                NetworkNodeId,
                List<NetworkNodeId>>();

        var initiatedPeers =
            new Dictionary<
                NetworkNodeId,
                List<NetworkNodeId>>();

        foreach (var edge in _edges)
        {
            AddPeer(
                peers,
                edge.First,
                edge.Second);

            AddPeer(
                peers,
                edge.Second,
                edge.First);

            AddPeer(
                initiatedPeers,
                edge.Initiator,
                edge.Initiator == edge.First
                    ? edge.Second
                    : edge.First);
        }

        _peers =
            Freeze(
                peers);

        _initiatedPeers =
            Freeze(
                initiatedPeers);
    }

    public IReadOnlyList<NetworkTopologyEdge> Edges =>
        _edges;

    public IReadOnlyList<NetworkNodeId> GetPeers(
        NetworkNodeId node)
    {
        ValidateNode(
            node);

        return _peers.TryGetValue(
            node,
            out var peers)
            ? peers
            : Array.Empty<NetworkNodeId>();
    }

    public IReadOnlyList<NetworkNodeId> GetInitiatedPeers(
        NetworkNodeId node)
    {
        ValidateNode(
            node);

        return _initiatedPeers.TryGetValue(
            node,
            out var peers)
            ? peers
            : Array.Empty<NetworkNodeId>();
    }

    private static void AddPeer(
        Dictionary<
            NetworkNodeId,
            List<NetworkNodeId>> peers,
        NetworkNodeId node,
        NetworkNodeId peer)
    {
        if (!peers.TryGetValue(
                node,
                out var nodePeers))
        {
            nodePeers =
                new List<NetworkNodeId>();

            peers.Add(
                node,
                nodePeers);
        }

        nodePeers.Add(
            peer);
    }

    private static Dictionary<
        NetworkNodeId,
        NetworkNodeId[]> Freeze(
        Dictionary<
            NetworkNodeId,
            List<NetworkNodeId>> source)
    {
        var result =
            new Dictionary<
                NetworkNodeId,
                NetworkNodeId[]>();

        foreach (var pair in source)
        {
            result.Add(
                pair.Key,
                pair.Value.ToArray());
        }

        return result;
    }

    private static void ValidateNode(
        NetworkNodeId node)
    {
        if (!node.IsValid)
        {
            throw new ArgumentException(
                "Network node ID must be valid.",
                nameof(node));
        }
    }
}
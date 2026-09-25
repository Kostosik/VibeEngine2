namespace Engine.Networking.Topology;

public sealed class StarTopology :
    INetworkTopology
{
    private readonly NetworkNodeId _center;

    public StarTopology(
        NetworkNodeId center)
    {
        if (!center.IsValid)
        {
            throw new ArgumentException(
                "Center node ID must be valid.",
                nameof(center));
        }

        _center =
            center;
    }

    public NetworkTopologyPlan Build(
        IReadOnlyList<NetworkNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        ValidateNodes(
            nodes,
            _center);

        var edges =
            new List<NetworkTopologyEdge>(
                Math.Max(
                    0,
                    nodes.Count - 1));

        foreach (var node in nodes)
        {
            if (node.Id == _center)
            {
                continue;
            }

            edges.Add(
                new NetworkTopologyEdge(
                    _center,
                    node.Id,
                    node.Id));
        }

        return new NetworkTopologyPlan(
            edges);
    }

    private static void ValidateNodes(
        IReadOnlyList<NetworkNode> nodes,
        NetworkNodeId center)
    {
        var ids =
            new HashSet<NetworkNodeId>();

        var centerFound =
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

            if (node.Id == center)
            {
                centerFound = true;
            }
        }

        if (!centerFound)
        {
            throw new InvalidOperationException(
                $"Center node '{center.Value}' does not exist in the topology.");
        }
    }
}
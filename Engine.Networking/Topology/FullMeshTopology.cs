namespace Engine.Networking.Topology;

public sealed class FullMeshTopology :
    INetworkTopology
{
    public NetworkTopologyPlan Build(
        IReadOnlyList<NetworkNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        ValidateNodes(nodes);

        var edges =
            new List<NetworkTopologyEdge>();

        for (var i = 0; i < nodes.Count; i++)
        {
            for (var j = i + 1; j < nodes.Count; j++)
            {
                var first =
                    nodes[i].Id;

                var second =
                    nodes[j].Id;

                var initiator =
                    first.Value < second.Value
                        ? first
                        : second;

                edges.Add(
                    new NetworkTopologyEdge(
                        first,
                        second,
                        initiator));
            }
        }

        return new NetworkTopologyPlan(
            edges);
    }

    private static void ValidateNodes(
        IReadOnlyList<NetworkNode> nodes)
    {
        var ids =
            new HashSet<NetworkNodeId>();

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
        }
    }
}
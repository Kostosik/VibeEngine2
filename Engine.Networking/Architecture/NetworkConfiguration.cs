using Engine.Networking.Authority;
using Engine.Networking.Topology;

namespace Engine.Networking.Architecture;

public sealed class NetworkConfiguration
{
    private readonly NetworkNode[] _nodes;

    public NetworkConfiguration(
        NetworkNode localNode,
        IReadOnlyList<NetworkNode> nodes,
        INetworkTopology topology,
        INetworkAuthority authority)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(authority);

        ValidateNodes(
            nodes,
            localNode.Id);

        if (authority.LocalNode !=
            localNode.Id)
        {
            throw new ArgumentException(
                "Network authority local node must match the configuration local node.",
                nameof(authority));
        }

        if (!nodes.Any(
                node =>
                    node.Id ==
                    authority.AuthorityNode))
        {
            throw new ArgumentException(
                "Authority node must exist in the network node list.",
                nameof(authority));
        }

        LocalNode =
            localNode;

        _nodes =
            nodes.ToArray();

        Topology =
            topology;

        Authority =
            authority;
    }

    public NetworkNode LocalNode { get; }

    public IReadOnlyList<NetworkNode> Nodes =>
        _nodes;

    public INetworkTopology Topology { get; }

    public INetworkAuthority Authority { get; }

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

            if (!ids.Add(node.Id))
            {
                throw new ArgumentException(
                    $"Network node ID '{node.Id.Value}' appears more than once.",
                    nameof(nodes));
            }

            if (node.Id ==
                localNodeId)
            {
                localNodeFound = true;
            }
        }

        if (!localNodeFound)
        {
            throw new ArgumentException(
                "Local node must exist in the network node list.",
                nameof(localNodeId));
        }
    }
}
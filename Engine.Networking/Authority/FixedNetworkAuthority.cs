using Engine.Networking.Topology;

namespace Engine.Networking.Authority;

public sealed class FixedNetworkAuthority :
    INetworkAuthority
{
    public FixedNetworkAuthority(
        NetworkNodeId localNode,
        NetworkNodeId authorityNode)
    {
        if (!localNode.IsValid)
        {
            throw new ArgumentException(
                "Local node ID must be valid.",
                nameof(localNode));
        }

        if (!authorityNode.IsValid)
        {
            throw new ArgumentException(
                "Authority node ID must be valid.",
                nameof(authorityNode));
        }

        LocalNode =
            localNode;

        AuthorityNode =
            authorityNode;
    }

    public NetworkNodeId LocalNode { get; }

    public NetworkNodeId AuthorityNode { get; }

    public bool HasAuthority =>
        LocalNode == AuthorityNode;
}
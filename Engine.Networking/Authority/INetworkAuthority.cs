using Engine.Networking.Topology;

namespace Engine.Networking.Authority;

public interface INetworkAuthority
{
    NetworkNodeId LocalNode { get; }

    NetworkNodeId AuthorityNode { get; }

    bool HasAuthority { get; }
}
using Engine.Networking.Connections;

namespace Engine.Networking.Topology;

public readonly record struct NetworkNode
{
    public NetworkNode(
        NetworkNodeId id,
        NetworkEndpoint endpoint)
    {
        if (!id.IsValid)
        {
            throw new ArgumentException(
                "Network node ID must be valid.",
                nameof(id));
        }

        Id = id;
        Endpoint = endpoint;
    }

    public NetworkNodeId Id { get; }

    public NetworkEndpoint Endpoint { get; }
}
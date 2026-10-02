using Engine.Networking.Authority;
using Engine.Networking.Connections;
using Engine.Networking.Sessions;
using Engine.Networking.Topology;

namespace Engine.Networking.Architecture;

public sealed class ClientServerNetwork
{
    private readonly NetworkArchitecture _architecture;

    private ClientServerNetwork(
        NetworkArchitecture architecture)
    {
        _architecture =
            architecture;
    }

    public bool IsStarted =>
        _architecture.IsStarted;

    public INetworkAuthority Authority =>
        _architecture.Authority;

    public NetworkNodeId ServerNode =>
        _architecture.AuthorityNode;

    public bool HasAuthority =>
        _architecture.HasAuthority;

    public IReadOnlyCollection<NetworkConnection> Connections =>
        _architecture.Connections;

    public NetworkTopologyPlan TopologyPlan =>
        _architecture.TopologyPlan;

    public static ClientServerNetwork CreateServer(
        NetworkSession session,
        NetworkNode server,
        IReadOnlyList<NetworkNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(
            session);

        ArgumentNullException.ThrowIfNull(
            nodes);

        return new ClientServerNetwork(
            new NetworkArchitecture(
                session,
                new NetworkConfiguration(
                    server,
                    nodes,
                    new ClientServerTopology(
                        server.Id),
                    new FixedNetworkAuthority(
                        server.Id,
                        server.Id))));
    }

    public static ClientServerNetwork CreateClient(
        NetworkSession session,
        NetworkNode client,
        NetworkNodeId serverNode,
        IReadOnlyList<NetworkNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(
            session);

        ArgumentNullException.ThrowIfNull(
            nodes);

        if (!serverNode.IsValid)
        {
            throw new ArgumentException(
                "Server node ID must be valid.",
                nameof(serverNode));
        }

        return new ClientServerNetwork(
            new NetworkArchitecture(
                session,
                new NetworkConfiguration(
                    client,
                    nodes,
                    new ClientServerTopology(
                        serverNode),
                    new FixedNetworkAuthority(
                        client.Id,
                        serverNode))));
    }

    public void Start()
    {
        _architecture.Start();
    }

    public void Update()
    {
        _architecture.Update();
    }

    public void Stop()
    {
        _architecture.Stop();
    }
}
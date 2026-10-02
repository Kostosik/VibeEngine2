using Engine.Networking.Authority;
using Engine.Networking.Connections;
using Engine.Networking.Sessions;
using Engine.Networking.Topology;

namespace Engine.Networking.Architecture;

public sealed class P2PNetwork
{
    private readonly NetworkArchitecture _architecture;

    private P2PNetwork(
        NetworkArchitecture architecture)
    {
        _architecture =
            architecture;
    }

    public bool IsStarted =>
        _architecture.IsStarted;

    public INetworkAuthority Authority =>
        _architecture.Authority;

    public bool HasAuthority =>
        _architecture.HasAuthority;

    public IReadOnlyCollection<NetworkConnection> Connections =>
        _architecture.Connections;

    public NetworkTopologyPlan TopologyPlan =>
        _architecture.TopologyPlan;

    public static P2PNetwork Create(
        NetworkSession session,
        NetworkNode localNode,
        IReadOnlyList<NetworkNode> nodes,
        INetworkAuthority authority)
    {
        ArgumentNullException.ThrowIfNull(
            session);

        ArgumentNullException.ThrowIfNull(
            nodes);

        ArgumentNullException.ThrowIfNull(
            authority);

        return new P2PNetwork(
            new NetworkArchitecture(
                session,
                new NetworkConfiguration(
                    localNode,
                    nodes,
                    new FullMeshP2PTopology(),
                    authority)));
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
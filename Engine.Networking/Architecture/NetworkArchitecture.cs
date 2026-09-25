using Engine.Networking.Connections;
using Engine.Networking.Sessions;
using Engine.Networking.Topology;

namespace Engine.Networking.Architecture;

public sealed class NetworkArchitecture
{
    private readonly NetworkSession _session;

    private readonly NetworkConfiguration _configuration;

    private readonly NetworkTopologyConnector _connector;

    private bool _started;

    private NetworkTopologyPlan? _topologyPlan;

    public NetworkArchitecture(
        NetworkSession session,
        NetworkConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(configuration);

        _session =
            session;

        _configuration =
            configuration;

        _connector =
            new NetworkTopologyConnector(
                session,
                configuration.LocalNode,
                configuration.Nodes);
    }

    public bool IsStarted =>
        _started;

    public NetworkConfiguration Configuration =>
        _configuration;

    public NetworkTopologyPlan TopologyPlan
    {
        get
        {
            if (_topologyPlan is null)
            {
                throw new InvalidOperationException(
                    "Network architecture has not been started.");
            }

            return _topologyPlan;
        }
    }

    public IReadOnlyCollection<NetworkConnection> Connections =>
        _session.Connections;

    public void Start()
    {
        if (_started)
        {
            throw new InvalidOperationException(
                "Network architecture is already started.");
        }

        var plan =
            _configuration.Topology.Build(
                _configuration.Nodes);

        _session.Start(
            _configuration.LocalNode.Endpoint);

        try
        {
            _connector.Apply(
                plan);

            _topologyPlan =
                plan;

            _started = true;
        }
        catch
        {
            _session.Stop();

            throw;
        }
    }

    public void Update()
    {
        EnsureStarted();

        _session.Update();
    }

    public void Stop()
    {
        if (!_started)
        {
            return;
        }

        _session.Stop();

        _topologyPlan =
            null;

        _started = false;
    }

    private void EnsureStarted()
    {
        if (!_started)
        {
            throw new InvalidOperationException(
                "Network architecture has not been started.");
        }
    }
}
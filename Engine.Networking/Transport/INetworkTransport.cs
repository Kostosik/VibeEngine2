using Engine.Networking.Connections;
using Engine.Networking.Packets;

namespace Engine.Networking.Transport;

public interface INetworkTransport :
    IDisposable
{
    NetworkTransportCapabilities Capabilities { get; }
    bool IsRunning { get; }

    void Start(
        NetworkEndpoint endpoint);

    void Stop();

    ConnectionId Connect(
        NetworkEndpoint endpoint);

    bool TryAccept(
        out ConnectionId connection,
        out NetworkEndpoint remoteEndpoint);

    void Disconnect(
        ConnectionId connection);

    bool TryReceiveDisconnect(
        out ConnectionId connection);

    bool Send(
        ConnectionId connection,
        NetworkPacket packet);

    bool TryReceive(
        out ConnectionId connection,
        out NetworkPacket packet);
}
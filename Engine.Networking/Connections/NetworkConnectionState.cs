namespace Engine.Networking.Connections;

public enum NetworkConnectionState : byte
{
    Disconnected = 0,
    Connecting = 1,
    Connected = 2,
    Disconnecting = 3
}
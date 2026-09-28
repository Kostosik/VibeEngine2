namespace Engine.Networking.Transport;

[Flags]
public enum NetworkTransportCapabilities : byte
{
    None = 0,
    Reliable = 1 << 0,
    Unreliable = 1 << 1
}
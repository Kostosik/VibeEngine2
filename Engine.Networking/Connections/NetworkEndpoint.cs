namespace Engine.Networking.Connections;

public readonly record struct NetworkEndpoint
{
    public NetworkEndpoint(
        string host,
        int port)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);

        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(
                nameof(port));
        }

        Host = host;
        Port = port;
    }

    public string Host { get; }

    public int Port { get; }
}
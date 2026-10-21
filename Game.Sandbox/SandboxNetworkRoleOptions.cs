
using System.Globalization;

namespace Game.Sandbox;

public enum SandboxNetworkRole
{
    Server,
    Client
}

public sealed record SandboxNetworkRoleOptions(
    SandboxNetworkRole Role,
    string BindHost,
    string Host,
    int Port,
    int LocalPort)
{
    public static bool TryParse(
        string[] args,
        out SandboxNetworkRoleOptions? options,
        out string? error)
    {
        options = null;
        error = null;

        var isServer = args.Any(arg =>
            string.Equals(
                arg,
                "--server",
                StringComparison.OrdinalIgnoreCase));

        var isClient = args.Any(arg =>
            string.Equals(
                arg,
                "--client",
                StringComparison.OrdinalIgnoreCase));

        if (!isServer && !isClient)
        {
            if (args.Length == 0)
            {
                return true;
            }

            error = $"Unsupported argument '{args[0]}'.";
            return false;
        }

        if (isServer && isClient)
        {
            error = "Choose either --server or --client.";
            return false;
        }

        var bindHost = "127.0.0.1";
        var host = "127.0.0.1";
        var port = 27015;
        int? localPort = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--server":
                case "--client":
                    break;

                case "--bind":
                    {
                        if (!isServer)
                        {
                            error = "--bind is only valid with --server.";
                            return false;
                        }

                        if (!TryReadValue(
                                args, ref i, "--bind",
                                out var value, out error))
                        {
                            return false;
                        }

                        bindHost = value;
                        break;
                    }

                case "--host":
                    {
                        if (!isClient)
                        {
                            error = "--host is only valid with --client.";
                            return false;
                        }

                        if (!TryReadValue(
                                args, ref i, "--host",
                                out var value, out error))
                        {
                            return false;
                        }

                        host = value;
                        break;
                    }

                case "--port":
                    {
                        if (!TryReadValue(
                                args, ref i, "--port",
                                out var value, out error))
                        {
                            return false;
                        }

                        if (!int.TryParse(
                                value,
                                NumberStyles.None,
                                CultureInfo.InvariantCulture,
                                out port) ||
                            port is < 1 or > 65535)
                        {
                            error = "--port must be between 1 and 65535.";
                            return false;
                        }

                        break;
                    }

                case "--local-port":
                    {
                        if (!isClient)
                        {
                            error = "--local-port is only valid with --client.";
                            return false;
                        }

                        if (!TryReadValue(
                                args, ref i, "--local-port",
                                out var value, out error))
                        {
                            return false;
                        }

                        if (!int.TryParse(
                                value,
                                NumberStyles.None,
                                CultureInfo.InvariantCulture,
                                out var parsedPort) ||
                            parsedPort is < 1 or > 65535)
                        {
                            error = "--local-port must be between 1 and 65535.";
                            return false;
                        }

                        localPort = parsedPort;
                        break;
                    }

                default:
                    error = $"Unknown multiplayer argument '{args[i]}'.";
                    return false;
            }
        }

        var role = isServer
            ? SandboxNetworkRole.Server
            : SandboxNetworkRole.Client;

        var effectiveLocalPort =
            localPort ?? (port < 65535 ? port + 1 : 0);

        if (role == SandboxNetworkRole.Client &&
            (effectiveLocalPort is < 1 or > 65535 ||
             effectiveLocalPort == port))
        {
            error =
                "The client local port must be valid and different " +
                "from the server port.";
            return false;
        }

        options = new SandboxNetworkRoleOptions(
            role,
            bindHost,
            host,
            port,
            effectiveLocalPort);

        return true;
    }

    private static bool TryReadValue(
        string[] args,
        ref int index,
        string option,
        out string value,
        out string? error)
    {
        value = string.Empty;
        error = null;

        if (index + 1 >= args.Length ||
            args[index + 1].StartsWith(
                "--",
                StringComparison.Ordinal))
        {
            error = $"Missing value for {option}.";
            return false;
        }

        value = args[++index];

        if (string.IsNullOrWhiteSpace(value))
        {
            error = $"Value for {option} cannot be empty.";
            return false;
        }

        return true;
    }
}

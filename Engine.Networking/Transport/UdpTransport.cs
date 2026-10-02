using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using Engine.Networking.Connections;
using Engine.Networking.Packets;

namespace Engine.Networking.Transport;

public sealed class UdpTransport :
    INetworkTransport
{
    private const ushort Magic =
        0x5642;

    private const byte Version =
        1;

    private const byte ConnectMessage =
        1;

    private const byte DisconnectMessage =
        2;

    private const byte PacketMessage =
        3;

    private const int HeaderSize =
        12;

    private const int PacketHeaderSize =
        19;

    private const int MaxDatagramSize =
        65507;

    private readonly Dictionary<
        ConnectionId,
        IPEndPoint> _connections =
        new();

    private readonly Dictionary<
        IPEndPoint,
        ConnectionId> _connectionsByEndpoint =
        new();

    private readonly Queue<
        PendingAccept> _accepts =
        new();

    private readonly Queue<
        ConnectionId> _disconnects =
        new();

    private readonly Queue<
        PendingPacket> _packets =
        new();

    private Socket? _socket;

    private NetworkEndpoint? _localEndpoint;

    private ulong _nextConnectionId =
        1;

    private bool _running;

    private bool _disposed;

    public NetworkTransportCapabilities Capabilities =>
        NetworkTransportCapabilities.Unreliable;

    public bool IsRunning =>
        _running;

    public void Start(
        NetworkEndpoint endpoint)
    {
        EnsureNotDisposed();

        if (_running)
        {
            throw new InvalidOperationException(
                "Transport is already running.");
        }

        var address =
            ResolveIPv4(
                endpoint.Host);

        var socket =
            new Socket(
                AddressFamily.InterNetwork,
                SocketType.Dgram,
                ProtocolType.Udp);

        try
        {
            socket.Blocking = false;

            socket.Bind(
                new IPEndPoint(
                    address,
                    endpoint.Port));

            _socket =
                socket;

            _localEndpoint =
                endpoint;

            _running =
                true;
        }
        catch
        {
            socket.Dispose();

            throw;
        }
    }

    public void Stop()
    {
        if (_disposed)
        {
            return;
        }

        if (!_running)
        {
            return;
        }

        var connections =
            _connections.Values.ToArray();

        foreach (var endpoint in
                 connections)
        {
            try
            {
                SendControl(
                    endpoint,
                    DisconnectMessage,
                    ConnectionId.Invalid);
            }
            catch (SocketException)
            {
            }
        }

        _connections.Clear();
        _connectionsByEndpoint.Clear();
        _accepts.Clear();
        _disconnects.Clear();
        _packets.Clear();

        _socket?.Dispose();

        _socket =
            null;

        _localEndpoint =
            null;

        _running =
            false;
    }

    public ConnectionId Connect(
        NetworkEndpoint endpoint)
    {
        EnsureRunning();

        var remote =
            ResolveEndpoint(
                endpoint);

        if (_connectionsByEndpoint.ContainsKey(
                remote))
        {
            throw new InvalidOperationException(
                $"A connection to '{endpoint.Host}:{endpoint.Port}' already exists.");
        }

        var connection =
            CreateConnection(
                remote);

        try
        {
            SendControl(
                remote,
                ConnectMessage,
                connection);
        }
        catch
        {
            RemoveConnection(
                connection);

            throw;
        }

        return connection;
    }

    public bool TryAccept(
        out ConnectionId connection,
        out NetworkEndpoint remoteEndpoint)
    {
        EnsureRunning();

        PumpIncoming();

        if (_accepts.TryDequeue(
                out var pending))
        {
            connection =
                pending.Connection;

            remoteEndpoint =
                pending.RemoteEndpoint;

            return true;
        }

        connection =
            ConnectionId.Invalid;

        remoteEndpoint =
            default;

        return false;
    }

    public void Disconnect(
        ConnectionId connection)
    {
        EnsureRunning();

        if (!_connections.TryGetValue(
                connection,
                out var endpoint))
        {
            return;
        }

        try
        {
            SendControl(
                endpoint,
                DisconnectMessage,
                connection);
        }
        finally
        {
            RemoveConnection(
                connection);
        }
    }

    public bool TryReceiveDisconnect(
        out ConnectionId connection)
    {
        EnsureRunning();

        PumpIncoming();

        if (_disconnects.TryDequeue(
                out connection))
        {
            return true;
        }

        connection =
            ConnectionId.Invalid;

        return false;
    }

    public bool Send(
        ConnectionId connection,
        NetworkPacket packet)
    {
        EnsureRunning();

        if (!_connections.TryGetValue(
                connection,
                out var endpoint))
        {
            return false;
        }

        if (packet.Channel !=
            NetworkChannel.Unreliable)
        {
            return false;
        }

        var payloadLength =
            packet.Payload.Length;

        if (payloadLength >
            MaxDatagramSize -
            PacketHeaderSize)
        {
            throw new ArgumentException(
                "Network packet payload is too large for UDP transport.",
                nameof(packet));
        }

        var data =
            new byte[
                PacketHeaderSize +
                payloadLength];

        WriteHeader(
            data,
            PacketMessage,
            connection);

        BinaryPrimitives.WriteUInt16LittleEndian(
            data.AsSpan(12, 2),
            packet.Id.Value);

        data[14] =
            (byte)packet.Channel;

        BinaryPrimitives.WriteInt32LittleEndian(
            data.AsSpan(15, 4),
            payloadLength);

        packet.Payload.Span.CopyTo(
            data.AsSpan(
                PacketHeaderSize,
                payloadLength));

        try
        {
            _socket!.SendTo(
                data,
                SocketFlags.None,
                endpoint);

            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    public bool TryReceive(
        out ConnectionId connection,
        out NetworkPacket packet)
    {
        EnsureRunning();

        PumpIncoming();

        if (_packets.TryDequeue(
                out var pending))
        {
            connection =
                pending.Connection;

            packet =
                pending.Packet;

            return true;
        }

        connection =
            ConnectionId.Invalid;

        packet =
            default;

        return false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();

        _disposed =
            true;
    }

    private void PumpIncoming()
    {
        var socket =
            _socket!;

        while (true)
        {
            var buffer =
                new byte[
                    MaxDatagramSize];

            EndPoint remote =
                new IPEndPoint(
                    IPAddress.Any,
                    0);

            int length;

            try
            {
                length =
                    socket.ReceiveFrom(
                        buffer,
                        SocketFlags.None,
                        ref remote);
            }
            catch (SocketException exception)
                when (exception.SocketErrorCode ==
                       SocketError.WouldBlock)
            {
                return;
            }

            if (remote is not IPEndPoint remoteEndpoint)
            {
                continue;
            }

            ProcessDatagram(
                buffer,
                length,
                remoteEndpoint);
        }
    }

    private void ProcessDatagram(
        byte[] buffer,
        int length,
        IPEndPoint remoteEndpoint)
    {
        if (length <
            HeaderSize)
        {
            return;
        }

        var span =
            buffer.AsSpan(
                0,
                length);

        if (BinaryPrimitives.ReadUInt16LittleEndian(
                span[..2]) !=
            Magic)
        {
            return;
        }

        if (span[2] !=
            Version)
        {
            return;
        }

        var kind =
            span[3];

        var remoteConnection =
            new ConnectionId(
                BinaryPrimitives.ReadUInt64LittleEndian(
                    span[4..12]));

        switch (kind)
        {
            case ConnectMessage:
                ProcessConnect(
                    remoteEndpoint,
                    remoteConnection);

                break;

            case DisconnectMessage:
                ProcessDisconnect(
                    remoteEndpoint);

                break;

            case PacketMessage:
                ProcessPacket(
                    span,
                    remoteEndpoint);

                break;
        }
    }

    private void ProcessConnect(
        IPEndPoint remoteEndpoint,
        ConnectionId remoteConnection)
    {
        if (!remoteConnection.IsValid)
        {
            return;
        }

        if (_connectionsByEndpoint.ContainsKey(
                remoteEndpoint))
        {
            return;
        }

        var connection =
            CreateConnection(
                remoteEndpoint);

        _accepts.Enqueue(
            new PendingAccept(
                connection,
                new NetworkEndpoint(
                    remoteEndpoint.Address.ToString(),
                    remoteEndpoint.Port)));
    }

    private void ProcessDisconnect(
        IPEndPoint remoteEndpoint)
    {
        if (!_connectionsByEndpoint.TryGetValue(
                remoteEndpoint,
                out var connection))
        {
            return;
        }

        RemoveConnection(
            connection);

        _disconnects.Enqueue(
            connection);
    }

    private void ProcessPacket(
        ReadOnlySpan<byte> span,
        IPEndPoint remoteEndpoint)
    {
        if (span.Length <
            PacketHeaderSize)
        {
            return;
        }

        if (!_connectionsByEndpoint.TryGetValue(
                remoteEndpoint,
                out var connection))
        {
            return;
        }

        var packetId =
            new PacketId(
                BinaryPrimitives.ReadUInt16LittleEndian(
                    span.Slice(
                        12,
                        2)));

        if (!packetId.IsValid)
        {
            return;
        }

        var channel =
            (NetworkChannel)span[14];

        if (channel is not
            NetworkChannel.Reliable and
            not NetworkChannel.Unreliable)
        {
            return;
        }

        var payloadLength =
            BinaryPrimitives.ReadInt32LittleEndian(
                span.Slice(
                    15,
                    4));

        if (payloadLength < 0 ||
            span.Length <
            PacketHeaderSize +
            payloadLength)
        {
            return;
        }

        var payload =
            span.Slice(
                    PacketHeaderSize,
                    payloadLength)
                .ToArray();

        _packets.Enqueue(
            new PendingPacket(
                connection,
                new NetworkPacket(
                    packetId,
                    channel,
                    payload)));
    }

    private ConnectionId CreateConnection(
        IPEndPoint endpoint)
    {
        var connection =
            new ConnectionId(
                _nextConnectionId++);

        _connections.Add(
            connection,
            endpoint);

        _connectionsByEndpoint.Add(
            endpoint,
            connection);

        return connection;
    }

    private void RemoveConnection(
        ConnectionId connection)
    {
        if (!_connections.Remove(
                connection,
                out var endpoint))
        {
            return;
        }

        _connectionsByEndpoint.Remove(
            endpoint);
    }

    private void SendControl(
        IPEndPoint endpoint,
        byte kind,
        ConnectionId connection)
    {
        var data =
            new byte[
                HeaderSize];

        WriteHeader(
            data,
            kind,
            connection);

        _socket!.SendTo(
            data,
            SocketFlags.None,
            endpoint);
    }

    private static void WriteHeader(
        byte[] data,
        byte kind,
        ConnectionId connection)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(
            data.AsSpan(0, 2),
            Magic);

        data[2] =
            Version;

        data[3] =
            kind;

        BinaryPrimitives.WriteUInt64LittleEndian(
            data.AsSpan(4, 8),
            connection.Value);
    }

    private static IPEndPoint ResolveEndpoint(
        NetworkEndpoint endpoint)
    {
        return new IPEndPoint(
            ResolveIPv4(
                endpoint.Host),
            endpoint.Port);
    }

    private static IPAddress ResolveIPv4(
        string host)
    {
        if (IPAddress.TryParse(
                host,
                out var parsed) &&
            parsed.AddressFamily ==
            AddressFamily.InterNetwork)
        {
            return parsed;
        }

        var addresses =
            Dns.GetHostAddresses(
                host);

        foreach (var address in
                 addresses)
        {
            if (address.AddressFamily ==
                AddressFamily.InterNetwork)
            {
                return address;
            }
        }

        throw new InvalidOperationException(
            $"Host '{host}' does not resolve to an IPv4 address.");
    }

    private void EnsureRunning()
    {
        EnsureNotDisposed();

        if (!_running)
        {
            throw new InvalidOperationException(
                "Transport is not running.");
        }
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }

    private readonly record struct PendingAccept(
        ConnectionId Connection,
        NetworkEndpoint RemoteEndpoint);

    private readonly record struct PendingPacket(
        ConnectionId Connection,
        NetworkPacket Packet);
}
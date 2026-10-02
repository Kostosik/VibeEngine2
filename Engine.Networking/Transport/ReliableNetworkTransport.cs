using System.Buffers.Binary;
using System.Diagnostics;
using Engine.Networking.Connections;
using Engine.Networking.Packets;

namespace Engine.Networking.Transport;

public sealed class ReliableNetworkTransport :
    INetworkTransport
{
    private const uint Magic =
        0x31544E52;

    private const byte Version =
        1;

    private const byte DataMessage =
        1;

    private const byte AckMessage =
        2;

    private const ushort DataPacketId =
        ushort.MaxValue;

    private const ushort AckPacketId =
        ushort.MaxValue - 1;

    private const int HeaderSize =
        14;

    private const int DataHeaderSize =
        21;

    private const int MaxBufferedPacketsPerConnection =
        1024;

    private static readonly TimeSpan RetransmitInterval =
        TimeSpan.FromMilliseconds(100);

    private readonly INetworkTransport _inner;

    private readonly Dictionary<
        ConnectionId,
        ConnectionState> _connections =
        new();

    private readonly Queue<
        PendingPacket> _incoming =
        new();

    private bool _disposed;

    public ReliableNetworkTransport(
        INetworkTransport inner)
    {
        ArgumentNullException.ThrowIfNull(
            inner);

        if ((inner.Capabilities &
             NetworkTransportCapabilities.Unreliable) == 0)
        {
            throw new ArgumentException(
                "Reliable network transport requires an unreliable-capable inner transport.",
                nameof(inner));
        }

        _inner =
            inner;
    }

    public NetworkTransportCapabilities Capabilities =>
        NetworkTransportCapabilities.Reliable |
        NetworkTransportCapabilities.Unreliable;

    public bool IsRunning =>
        _inner.IsRunning;

    public void Start(
        NetworkEndpoint endpoint)
    {
        EnsureNotDisposed();

        _inner.Start(
            endpoint);
    }

    public void Stop()
    {
        if (_disposed)
        {
            return;
        }

        _connections.Clear();
        _incoming.Clear();

        _inner.Stop();
    }

    public ConnectionId Connect(
        NetworkEndpoint endpoint)
    {
        EnsureRunning();

        var connection =
            _inner.Connect(
                endpoint);

        _connections[connection] =
            new ConnectionState();

        return connection;
    }

    public bool TryAccept(
        out ConnectionId connection,
        out NetworkEndpoint remoteEndpoint)
    {
        EnsureRunning();

        if (_inner.TryAccept(
                out connection,
                out remoteEndpoint))
        {
            _connections.TryAdd(
                connection,
                new ConnectionState());

            return true;
        }

        return false;
    }

    public void Disconnect(
        ConnectionId connection)
    {
        EnsureRunning();

        _connections.Remove(
            connection);

        _inner.Disconnect(
            connection);
    }

    public bool TryReceiveDisconnect(
        out ConnectionId connection)
    {
        EnsureRunning();

        if (_inner.TryReceiveDisconnect(
                out connection))
        {
            _connections.Remove(
                connection);

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
                out var state))
        {
            return false;
        }

        if (packet.Channel ==
            NetworkChannel.Unreliable)
        {
            return _inner.Send(
                connection,
                packet);
        }

        if (packet.Channel !=
            NetworkChannel.Reliable)
        {
            return false;
        }

        var sequence =
            state.NextSendSequence;

        var frame =
            CreateDataFrame(
                sequence,
                packet);

        var wrappedPacket =
            new NetworkPacket(
                new PacketId(DataPacketId),
                NetworkChannel.Unreliable,
                frame);

        if (!_inner.Send(
                connection,
                wrappedPacket))
        {
            return false;
        }

        state.Pending.Add(
            sequence,
            new PendingSend(
                frame,
                Stopwatch.GetTimestamp()));

        state.NextSendSequence++;

        return true;
    }

    public bool TryReceive(
        out ConnectionId connection,
        out NetworkPacket packet)
    {
        EnsureRunning();

        if (_incoming.TryDequeue(
                out var pending))
        {
            connection =
                pending.Connection;

            packet =
                pending.Packet;

            return true;
        }

        RetransmitExpired();

        PumpIncoming();

        if (_incoming.TryDequeue(
                out pending))
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

        _inner.Dispose();

        _disposed =
            true;
    }

    private void PumpIncoming()
    {
        while (
            _inner.TryReceive(
                out var connection,
                out var packet))
        {
            if (!_connections.ContainsKey(
                    connection))
            {
                continue;
            }

            if (packet.Id ==
                new PacketId(DataPacketId) &&
                TryProcessData(
                    connection,
                    packet))
            {
                continue;
            }

            if (packet.Id ==
                new PacketId(AckPacketId) &&
                TryProcessAck(
                    connection,
                    packet))
            {
                continue;
            }

            _incoming.Enqueue(
                new PendingPacket(
                    connection,
                    packet));
        }
    }

    private bool TryProcessData(
        ConnectionId connection,
        NetworkPacket packet)
    {
        if (packet.Channel !=
            NetworkChannel.Unreliable)
        {
            return false;
        }

        var span =
            packet.Payload.Span;

        if (span.Length <
            DataHeaderSize)
        {
            return false;
        }

        if (!ValidateHeader(
                span,
                DataMessage))
        {
            return false;
        }

        var sequence =
            BinaryPrimitives.ReadUInt64LittleEndian(
                span.Slice(6, 8));

        var packetId =
            new PacketId(
                BinaryPrimitives.ReadUInt16LittleEndian(
                    span.Slice(14, 2)));

        if (!packetId.IsValid)
        {
            return true;
        }

        var channel =
            (NetworkChannel)span[16];

        if (channel !=
            NetworkChannel.Reliable)
        {
            return true;
        }

        var payloadLength =
            BinaryPrimitives.ReadInt32LittleEndian(
                span.Slice(17, 4));

        if (payloadLength < 0 ||
            span.Length !=
            DataHeaderSize +
            payloadLength)
        {
            return true;
        }

        SendAck(
            connection,
            sequence);

        var state =
            _connections[connection];

        if (sequence <
            state.NextReceiveSequence)
        {
            return true;
        }

        if (sequence >
            state.NextReceiveSequence)
        {
            if (state.Buffered.Count <
                MaxBufferedPacketsPerConnection)
            {
                state.Buffered.TryAdd(
                    sequence,
                    new NetworkPacket(
                        packetId,
                        channel,
                        span.Slice(
                                DataHeaderSize,
                                payloadLength)
                            .ToArray()));
            }

            return true;
        }

        _incoming.Enqueue(
            new PendingPacket(
                connection,
                new NetworkPacket(
                    packetId,
                    channel,
                    span.Slice(
                            DataHeaderSize,
                            payloadLength)
                        .ToArray())));

        state.NextReceiveSequence++;

        while (
            state.Buffered.Remove(
                state.NextReceiveSequence,
                out var bufferedPacket))
        {
            _incoming.Enqueue(
                new PendingPacket(
                    connection,
                    bufferedPacket));

            state.NextReceiveSequence++;
        }

        return true;
    }

    private bool TryProcessAck(
        ConnectionId connection,
        NetworkPacket packet)
    {
        if (packet.Channel !=
            NetworkChannel.Unreliable)
        {
            return false;
        }

        var span =
            packet.Payload.Span;

        if (span.Length !=
            HeaderSize)
        {
            return false;
        }

        if (!ValidateHeader(
                span,
                AckMessage))
        {
            return false;
        }

        var sequence =
            BinaryPrimitives.ReadUInt64LittleEndian(
                span.Slice(6, 8));

        _connections[connection]
            .Pending
            .Remove(sequence);

        return true;
    }

    private void SendAck(
        ConnectionId connection,
        ulong sequence)
    {
        var frame =
            new byte[
                HeaderSize];

        WriteHeader(
            frame,
            AckMessage,
            sequence);

        _inner.Send(
            connection,
            new NetworkPacket(
                new PacketId(AckPacketId),
                NetworkChannel.Unreliable,
                frame));
    }

    private void RetransmitExpired()
    {
        var now =
            Stopwatch.GetTimestamp();

        foreach (var pair in
                 _connections)
        {
            var connection =
                pair.Key;

            var state =
                pair.Value;

            foreach (var pending in
                     state.Pending)
            {
                if (!HasElapsed(
                        pending.Value.LastSentTimestamp,
                        now))
                {
                    continue;
                }

                var packet =
                    new NetworkPacket(
                        new PacketId(DataPacketId),
                        NetworkChannel.Unreliable,
                        pending.Value.Frame);

                if (_inner.Send(
                        connection,
                        packet))
                {
                    pending.Value.LastSentTimestamp =
                        now;
                }
            }
        }
    }

    private static bool HasElapsed(
        long start,
        long end)
    {
        var elapsed =
            (double)(end - start) /
            Stopwatch.Frequency;

        return elapsed >=
               RetransmitInterval.TotalSeconds;
    }

    private static byte[] CreateDataFrame(
        ulong sequence,
        NetworkPacket packet)
    {
        if (packet.Payload.Length >
            int.MaxValue)
        {
            throw new ArgumentException(
                "Network packet payload is too large.",
                nameof(packet));
        }

        var payloadLength =
            packet.Payload.Length;

        var frame =
            new byte[
                DataHeaderSize +
                payloadLength];

        WriteHeader(
            frame,
            DataMessage,
            sequence);

        BinaryPrimitives.WriteUInt16LittleEndian(
            frame.AsSpan(14, 2),
            packet.Id.Value);

        frame[16] =
            (byte)packet.Channel;

        BinaryPrimitives.WriteInt32LittleEndian(
            frame.AsSpan(17, 4),
            payloadLength);

        packet.Payload.Span.CopyTo(
            frame.AsSpan(
                DataHeaderSize,
                payloadLength));

        return frame;
    }

    private static void WriteHeader(
        byte[] frame,
        byte messageType,
        ulong sequence)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(
            frame.AsSpan(0, 4),
            Magic);

        frame[4] =
            Version;

        frame[5] =
            messageType;

        BinaryPrimitives.WriteUInt64LittleEndian(
            frame.AsSpan(6, 8),
            sequence);
    }

    private static bool ValidateHeader(
        ReadOnlySpan<byte> frame,
        byte expectedMessageType)
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(
                   frame[..4]) ==
               Magic &&
               frame[4] ==
               Version &&
               frame[5] ==
               expectedMessageType;
    }

    private void EnsureRunning()
    {
        EnsureNotDisposed();

        if (!_inner.IsRunning)
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

    private sealed class ConnectionState
    {
        public ulong NextSendSequence =
            1;

        public ulong NextReceiveSequence =
            1;

        public Dictionary<
            ulong,
            PendingSend> Pending
        { get; } =
            new();

        public SortedDictionary<
            ulong,
            NetworkPacket> Buffered
        { get; } =
            new();
    }

    private sealed class PendingSend
    {
        public PendingSend(
            byte[] frame,
            long lastSentTimestamp)
        {
            Frame =
                frame;

            LastSentTimestamp =
                lastSentTimestamp;
        }

        public byte[] Frame { get; }

        public long LastSentTimestamp { get; set; }
    }

    private readonly record struct PendingPacket(
        ConnectionId Connection,
        NetworkPacket Packet);
}
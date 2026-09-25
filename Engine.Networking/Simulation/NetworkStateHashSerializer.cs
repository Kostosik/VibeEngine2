using System.Buffers.Binary;
using Engine.Core.Determinism;
using Engine.Core.Time;

namespace Engine.Networking.Simulation;

public static class NetworkStateHashSerializer
{
    private const int PayloadSize = sizeof(ulong) * 2;

    public static byte[] Serialize(
        NetworkStateHashMessage message)
    {
        var payload =
            new byte[PayloadSize];

        BinaryPrimitives.WriteUInt64LittleEndian(
            payload.AsSpan(0, sizeof(ulong)),
            message.Tick.Value);

        BinaryPrimitives.WriteUInt64LittleEndian(
            payload.AsSpan(sizeof(ulong), sizeof(ulong)),
            message.Hash.Value);

        return payload;
    }

    public static NetworkStateHashMessage Deserialize(
        ReadOnlySpan<byte> payload)
    {
        if (payload.Length != PayloadSize)
        {
            throw new InvalidDataException(
                $"Invalid state hash payload size. " +
                $"Expected {PayloadSize} bytes, " +
                $"received {payload.Length}.");
        }

        var tick =
            BinaryPrimitives.ReadUInt64LittleEndian(
                payload[..sizeof(ulong)]);

        var hash =
            BinaryPrimitives.ReadUInt64LittleEndian(
                payload.Slice(
                    sizeof(ulong),
                    sizeof(ulong)));

        return new NetworkStateHashMessage(
            new Tick(tick),
            new DeterministicStateHash(hash));
    }
}
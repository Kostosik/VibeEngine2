using Engine.Core.Replays;
using Engine.Core.Time;
using System.Text.Json;

namespace Engine.Networking.Simulation;

public static class NetworkCommandSerializer
{
    private static readonly JsonSerializerOptions JsonOptions =
        new();

    public static byte[] Serialize(
        NetworkCommandBatch batch,
        ReplayCommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(registry);

        var data =
            new NetworkCommandBatchData
            {
                Tick =
                    batch.Tick,

                Commands =
    batch.Commands
        .Select(
            command =>
            {
                var data =
                    registry.SerializeCommand(
                        command);

                return new NetworkCommandData
                {
                    Type = data.Type,
                    Payload = data.Payload
                };
            })
        .ToList()
            };

        return JsonSerializer.SerializeToUtf8Bytes(
            data,
            JsonOptions);
    }

    public static NetworkCommandBatch Deserialize(
        ReadOnlySpan<byte> payload,
        ReplayCommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        if (payload.IsEmpty)
        {
            throw new InvalidDataException(
                "Network command payload is empty.");
        }

        var data =
            JsonSerializer.Deserialize<
                NetworkCommandBatchData>(
                    payload);

        if (data is null)
        {
            throw new InvalidDataException(
                "Network command payload is invalid.");
        }

        var batch =
            new NetworkCommandBatch(
                data.Tick);

        foreach (var command in data.Commands)
        {
            batch.Add(
                registry.DeserializeCommand(
                    new ReplayCommandData(
                        command.Type,
                        command.Payload)));
        }

        return batch;
    }

    private sealed class NetworkCommandBatchData
    {
        public Tick Tick { get; set; }

        public List<NetworkCommandData> Commands { get; set; } =
            new();
    }

    private sealed class NetworkCommandData
    {
        public string Type { get; set; } = string.Empty;

        public string Payload { get; set; } = string.Empty;
    }
}
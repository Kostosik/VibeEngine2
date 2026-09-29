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
        ArgumentNullException.ThrowIfNull(
            registry);

        if (payload.IsEmpty)
        {
            throw new InvalidDataException(
                "Network command payload is empty.");
        }

        NetworkCommandBatchData? data;

        try
        {
            data =
                JsonSerializer.Deserialize<
                    NetworkCommandBatchData>(
                    payload);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "Network command payload contains invalid JSON.",
                exception);
        }

        if (data is null)
        {
            throw new InvalidDataException(
                "Network command payload is invalid.");
        }

        if (data.Commands is null)
        {
            throw new InvalidDataException(
                "Network command payload contains no command list.");
        }

        var batch =
            new NetworkCommandBatch(
                data.Tick);

        foreach (var command in data.Commands)
        {
            if (command is null)
            {
                throw new InvalidDataException(
                    "Network command payload contains a null command.");
            }

            try
            {
                batch.Add(
                    registry.DeserializeCommand(
                        new ReplayCommandData(
                            command.Type,
                            command.Payload)));
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception exception)
                when (exception is
                    ArgumentException or
                    JsonException)
            {
                throw new InvalidDataException(
                    $"Network command '{command.Type}' is invalid.",
                    exception);
            }
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
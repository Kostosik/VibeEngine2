using Engine.Core.Commands;
using Engine.Core.Time;
using System.Text.Json;

namespace Engine.Core.Replays;

public static class ReplaySerializer
{
    private const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            WriteIndented = true
        };

    public static void Save(
        Replay replay,
        string path,
        ReplayCommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(replay);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var file =
            new ReplayFile
            {
                Version =
                    CurrentVersion,

                Frames =
                    replay.Frames
                        .Select(
                            frame =>
                                new ReplayFrameFile
                                {
                                    Tick =
                                        frame.Tick,

                                    Commands =
                                        frame.Commands
                                            .Select(
                                                registry.Serialize)
                                            .ToList()
                                })
                        .ToList()
            };

        var json =
            JsonSerializer.Serialize(
                file,
                JsonOptions);

        File.WriteAllText(
            path,
            json);
    }

    public static Replay Load(
        string path,
        ReplayCommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                "Replay file was not found.",
                path);
        }

        var json =
            File.ReadAllText(
                path);

        var file =
            JsonSerializer.Deserialize<ReplayFile>(
                json);

        if (file is null)
        {
            throw new InvalidDataException(
                "Replay file is empty or invalid.");
        }

        if (file.Version != CurrentVersion)
        {
            throw new InvalidDataException(
                $"Unsupported replay version '{file.Version}'.");
        }

        var replay =
            new Replay();

        foreach (var frame in file.Frames)
        {
            foreach (var commandData in frame.Commands)
            {
                replay
                    .GetOrCreateFrame(frame.Tick)
                    .Add(
                        registry.Deserialize(
                            commandData));
            }
        }

        return replay;
    }

    private sealed class ReplayFile
    {
        public int Version { get; set; }

        public List<ReplayFrameFile> Frames { get; set; } = new();
    }

    private sealed class ReplayFrameFile
    {
        public Tick Tick { get; set; }

        public List<ReplayCommandData> Commands { get; set; } = new();
    }
}
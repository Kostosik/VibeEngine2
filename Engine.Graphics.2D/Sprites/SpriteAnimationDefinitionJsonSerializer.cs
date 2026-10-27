using System.Text.Json;

namespace Engine.Graphics2D.Sprites;

public sealed class SpriteAnimationDefinitionJsonSerializer
{
    public const int CurrentFormatVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public string Serialize(SpriteAnimationDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var document = new AnimationDocument
        {
            FormatVersion = CurrentFormatVersion,
            FrameDurationSeconds = definition.FrameDurationSeconds,
            Loop = definition.Loop,
            Frames = definition.Frames
                .Select(static frame => new FrameDocument
                {
                    TexturePath = frame.TexturePath
                })
                .ToList()
        };

        return JsonSerializer.Serialize(document, Options);
    }

    public SpriteAnimationDefinition Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var document =
            JsonSerializer.Deserialize<AnimationDocument>(
                json,
                Options)
            ?? throw new JsonException(
                "Sprite animation document is empty.");

        if (document.FormatVersion != CurrentFormatVersion)
        {
            throw new NotSupportedException(
                $"Sprite animation format version " +
                $"'{document.FormatVersion}' is not supported.");
        }

        if (document.Frames is null)
        {
            throw new JsonException(
                "Sprite animation frames collection is null.");
        }

        var frames = document.Frames
            .Select(static frame =>
                new SpriteAnimationFrameDefinition(
                    frame?.TexturePath
                    ?? throw new JsonException(
                        "Sprite animation frame is null or has no texture path.")))
            .ToArray();

        return new SpriteAnimationDefinition(
            frames,
            document.FrameDurationSeconds,
            document.Loop);
    }

    private sealed class AnimationDocument
    {
        public int FormatVersion { get; set; } =
            CurrentFormatVersion;

        public float FrameDurationSeconds { get; set; } = 0.1f;

        public bool Loop { get; set; } = true;

        public List<FrameDocument?>? Frames { get; set; } = new();
    }

    private sealed class FrameDocument
    {
        public string? TexturePath { get; set; }
    }
}
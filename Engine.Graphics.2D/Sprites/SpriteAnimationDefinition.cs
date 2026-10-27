namespace Engine.Graphics2D.Sprites;

public sealed record SpriteAnimationFrameDefinition
{
    public SpriteAnimationFrameDefinition(string texturePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(texturePath);

        var normalized = texturePath
            .Replace('\\', '/')
            .Trim('/');

        if (Path.IsPathRooted(texturePath) ||
            normalized.Contains(':') ||
            normalized.Split('/').Any(
                static segment => segment is "." or ".."))
        {
            throw new ArgumentException(
                "Texture path must be relative to the asset root.",
                nameof(texturePath));
        }

        TexturePath = normalized;
    }

    public string TexturePath { get; }
}

public sealed class SpriteAnimationDefinition
{
    private readonly IReadOnlyList<SpriteAnimationFrameDefinition> _frames;

    public SpriteAnimationDefinition(
        IReadOnlyList<SpriteAnimationFrameDefinition>? frames = null,
        float frameDurationSeconds = 0.1f,
        bool loop = true)
    {
        if (!float.IsFinite(frameDurationSeconds) ||
            frameDurationSeconds <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(frameDurationSeconds));
        }

        var frameArray = frames?.ToArray()
            ?? Array.Empty<SpriteAnimationFrameDefinition>();

        if (frameArray.Any(static frame => frame is null))
        {
            throw new ArgumentException(
                "Sprite animation frames cannot contain null entries.",
                nameof(frames));
        }

        _frames = Array.AsReadOnly(frameArray);

        FrameDurationSeconds = frameDurationSeconds;
        Loop = loop;
    }

    public IReadOnlyList<SpriteAnimationFrameDefinition> Frames =>
        _frames;

    public float FrameDurationSeconds { get; }

    public bool Loop { get; }
}
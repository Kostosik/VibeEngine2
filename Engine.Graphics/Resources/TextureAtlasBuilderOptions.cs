namespace Engine.Graphics.Resources;

public sealed record TextureAtlasBuilderOptions
{
    public int MaxWidth { get; init; } = 2048;

    public int MaxHeight { get; init; } = 2048;

    /// <summary>
    /// Empty space reserved around each sprite.
    /// </summary>
    public int Padding { get; init; }

    /// <summary>
    /// Number of pixels copied outward from sprite edges.
    /// Must not exceed Padding.
    /// </summary>
    public int Extrusion { get; init; }
}
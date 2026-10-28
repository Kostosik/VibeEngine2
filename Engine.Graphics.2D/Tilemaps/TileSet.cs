using Engine.Core.Math;
using Engine.Graphics.Resources;

namespace Engine.Graphics.Tilemaps;

public readonly record struct TileVisual(
    TextureHandle Texture,
    Rectangle UV);

public sealed class TileSet
{
    private readonly Dictionary<uint, TileVisual> _visuals = new();

    public int Count =>
        _visuals.Count;

    public void Set(
        uint tileValue,
        TextureAtlas atlas,
        int atlasIndex)
    {
        ArgumentNullException.ThrowIfNull(atlas);

        var region = atlas.GetRegion(atlasIndex);

        Set(
            tileValue,
            atlas.Texture,
            region.UV);
    }

    public void Set(
        uint tileValue,
        TextureHandle texture,
        Rectangle uv)
    {
        if (tileValue == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tileValue),
                "Tile value 0 is reserved for empty cells.");
        }

        if (!texture.IsValid)
        {
            throw new ArgumentException(
                "Texture handle is invalid.",
                nameof(texture));
        }

        _visuals[tileValue] =
            new TileVisual(
                texture,
                uv);
    }

    public bool TryGet(
        uint tileValue,
        out TileVisual visual)
    {
        return _visuals.TryGetValue(
            tileValue,
            out visual);
    }

    public bool Remove(
        uint tileValue)
    {
        return _visuals.Remove(tileValue);
    }

    public void Clear()
    {
        _visuals.Clear();
    }
}
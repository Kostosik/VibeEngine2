using System.Collections.ObjectModel;

namespace Engine.Graphics.Resources;

public sealed class TextureAtlasBuildResult
{
    private readonly AtlasRegion[] _orderedRegions;

    internal TextureAtlasBuildResult(
        TextureData texture,
        Dictionary<string, AtlasRegion> regions,
        AtlasRegion[] orderedRegions)
    {
        Texture = texture;
        Regions = new ReadOnlyDictionary<string, AtlasRegion>(
            regions);

        _orderedRegions = orderedRegions;
    }

    public TextureData Texture { get; }

    public IReadOnlyDictionary<string, AtlasRegion> Regions { get; }

    public AtlasRegion GetRegion(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!Regions.TryGetValue(name, out var region))
        {
            throw new KeyNotFoundException(
                $"Atlas region '{name}' was not found.");
        }

        return region;
    }

    public TextureAtlas CreateAtlas(TextureHandle textureHandle)
    {
        return new TextureAtlas(
            textureHandle,
            Texture.Description,
            _orderedRegions);
    }
}
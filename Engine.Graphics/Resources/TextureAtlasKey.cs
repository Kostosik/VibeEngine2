using Engine.Core.Assets;

namespace Engine.Graphics.Resources;

internal readonly record struct TextureAtlasKey(
    AssetPath Path,
    int TileWidth,
    int TileHeight);
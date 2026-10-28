using Engine.Graphics.Rendering;

namespace Engine.Graphics.Tilemaps;

public struct TilemapComponent
{
    public TilemapComponent()
    {
        TilemapPath = string.Empty;
        Layer = RenderLayers.World;
    }

    public TilemapComponent(
        string tilemapPath,
        int layer = RenderLayers.World)
    {
        ArgumentNullException.ThrowIfNull(tilemapPath);

        TilemapPath = tilemapPath;
        Layer = layer;
    }

    public string TilemapPath { get; set; }

    public int Layer { get; set; }
}
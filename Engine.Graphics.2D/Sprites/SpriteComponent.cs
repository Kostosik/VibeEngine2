using Engine.Graphics.Rendering;

namespace Engine.Graphics2D.Sprites;

public struct SpriteComponent
{
    public SpriteComponent()
    {
        TexturePath = string.Empty;
        Width = 1.0f;
        Height = 1.0f;
        Layer = RenderLayers.World;
    }

    public SpriteComponent(
        string texturePath,
        float width = 1.0f,
        float height = 1.0f,
        int layer = RenderLayers.World)
    {
        ArgumentNullException.ThrowIfNull(
            texturePath);

        TexturePath = texturePath;
        Width = width;
        Height = height;
        Layer = layer;
    }

    public string TexturePath { get; set; }

    public float Width { get; set; }

    public float Height { get; set; }

    public int Layer { get; set; }
}
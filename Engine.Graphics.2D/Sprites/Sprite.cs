using Engine.Core.Math;
using Engine.Graphics.Resources;
using Engine.Graphics.Materials;

namespace Engine.Graphics2D.Sprites;

public readonly record struct Sprite
{
    public Vector2 NormalScale { get; init; } = Vector2.One;
    // Старый API сохраняется.
    public Sprite(
        TextureHandle Texture,
        Rectangle UV,
        Vector2 Size,
        int Layer = 0)
        : this(
            SpriteMaterial.AlbedoOnly(Texture),
            UV,
            Size,
            Layer)
    {
    }

    // Новый API с материалом.
    public Sprite(
        SpriteMaterial Material,
        Rectangle UV,
        Vector2 Size,
        int Layer = 0)
    {
        this.Material = Material;
        this.UV = UV;
        this.Size = Size;
        this.Layer = Layer;
    }

    public SpriteMaterial Material { get; init; }

    // Совместимость со старым API.
    public TextureHandle Texture
    {
        get => Material.Albedo;

        init => Material = Material with
        {
            Albedo = value
        };
    }

    public Rectangle UV { get; init; }

    public Vector2 Size { get; init; }

    public int Layer { get; init; }

    // Сохраняем прежнюю деконструкцию.
    public void Deconstruct(
        out TextureHandle texture,
        out Rectangle uv,
        out Vector2 size,
        out int layer)
    {
        texture = Texture;
        uv = UV;
        size = Size;
        layer = Layer;
    }
}
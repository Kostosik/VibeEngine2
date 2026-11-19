using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Graphics.Materials;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;

namespace Engine.Graphics2D.Commands;

public sealed record DrawWorldTextureCommand(
    TextureHandle Texture,
    Vector2 Position,
    Vector2 Size,
    Rectangle UV,
    int Layer = RenderLayers.World,
    float RotationDegrees = 0.0f) : IRenderCommand
{
    public SpriteMaterial Material { get; init; } =
        SpriteMaterial.AlbedoOnly(Texture);
}
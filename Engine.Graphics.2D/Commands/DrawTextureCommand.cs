using Engine.Core.Math;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;

namespace Engine.Graphics.Commands;

public sealed record DrawTextureCommand(
    TextureHandle Texture,
    Vector2 Position,
    Vector2 Size,
    Rectangle UV,
    int Layer = RenderLayers.World) : IRenderCommand;
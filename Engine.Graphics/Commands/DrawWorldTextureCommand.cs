using Engine.Core.Math;
using Engine.Graphics.Resources;

namespace Engine.Graphics.Commands;

public sealed record DrawWorldTextureCommand(
    TextureHandle Texture,
    Vector2 Position,
    Vector2 Size,
    Rectangle UV,
    int Layer = 0) : IRenderCommand;
using Engine.Core.Math;
using Engine.Graphics.Resources;

namespace Engine.Graphics.Commands;

public readonly record struct DrawUiTextureCommand(
    TextureHandle Texture,
    Vector2 Position,
    Vector2 Size,
    Rectangle UV,
    int Layer = 1000,
    Rectangle? ClipRect = null)
    : IRenderCommand;
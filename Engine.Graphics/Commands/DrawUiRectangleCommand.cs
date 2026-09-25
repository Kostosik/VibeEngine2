using Engine.Core.Math;

namespace Engine.Graphics.Commands;

public readonly record struct DrawUiRectangleCommand(
    Vector2 Position,
    Vector2 Size,
    UiColor Color,
    bool Filled = true,
    int Layer = 1000,
    Rectangle? ClipRect = null)
    : IRenderCommand;
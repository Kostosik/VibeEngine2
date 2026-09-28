using Engine.Core.Math;
using Engine.Graphics.Rendering;

namespace Engine.Graphics.Commands;

public readonly record struct DrawUiRectangleCommand(
    Vector2 Position,
    Vector2 Size,
    UiColor Color,
    bool Filled = true,
    int Layer = RenderLayers.Ui,
    Rectangle? ClipRect = null)
    : IRenderCommand;
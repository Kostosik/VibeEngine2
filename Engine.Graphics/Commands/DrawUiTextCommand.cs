using Engine.Core.Math;
using Engine.Graphics.Fonts;

namespace Engine.Graphics.Commands;

public readonly record struct DrawUiTextCommand(
    string Text,
    FontHandle Font,
    Vector2 Position,
    float FontSize,
    UiColor Color,
    int Layer = 1001,
    Rectangle? ClipRect = null)
    : IRenderCommand;
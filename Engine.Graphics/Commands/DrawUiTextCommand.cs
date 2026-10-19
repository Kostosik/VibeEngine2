using Engine.Core.Math;
using Engine.Graphics.Fonts;
using Engine.Graphics.Rendering;

namespace Engine.Graphics.Commands;

public readonly record struct DrawUiTextCommand(
    string Text,
    FontHandle Font,
    Vector2 Position,
    float FontSize,
    UiColor Color,
    int Layer = RenderLayers.Ui + 1,
    Rectangle? ClipRect = null,
    float RotationDegrees = 0.0f,
    ClipRegion? ClipRegion = null)
    : IRenderCommand;
using Engine.Core.Math;
using Engine.Graphics.DebugGraphics;
using Engine.Graphics.Rendering;

namespace Engine.Graphics.Commands;

public readonly record struct DrawDebugTextCommand(
    string Text,
    Vector2 Position,
    DebugColor Color,
    float Scale = 2.0f,
    DebugRenderSpace Space = DebugRenderSpace.Screen,
    int Layer = RenderLayers.DebugText,
    DebugTextAlignment Alignment = DebugTextAlignment.Left,
    DebugColor? BackgroundColor = null,
    Vector2 BackgroundPadding = default,
    DebugColor? OutlineColor = null,
    float OutlineThickness = 1.0f,
    float LetterSpacing = 1.0f,
    float LineSpacing = 1.0f)
    : IRenderCommand;
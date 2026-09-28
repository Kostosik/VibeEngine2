using Engine.Core.Math;
using Engine.Graphics.DebugGraphics;
using Engine.Graphics.Rendering;

namespace Engine.Graphics.Commands;

public sealed record DrawDebugRectangleCommand(
    Vector2 Position,
    Vector2 Size,
    DebugColor Color,
    bool Filled = false,
    DebugRenderSpace Space = DebugRenderSpace.World,
    int Layer = RenderLayers.Debug) : IRenderCommand;
using Engine.Core.Math;
using Engine.Graphics.DebugGraphics;
using Engine.Graphics.Rendering;

namespace Engine.Graphics.Commands;

public sealed record DrawDebugCircleCommand(
    Vector2 Center,
    float Radius,
    DebugColor Color,
    bool Filled = false,
    int Segments = 16,
    DebugRenderSpace Space = DebugRenderSpace.World,
    int Layer = RenderLayers.Debug) : IRenderCommand;
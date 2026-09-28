using Engine.Core.Math;
using Engine.Graphics.DebugGraphics;
using Engine.Graphics.Rendering;

namespace Engine.Graphics.Commands;

public sealed record DrawDebugLineCommand(
    Vector2 Start,
    Vector2 End,
    DebugColor Color,
    DebugRenderSpace Space = DebugRenderSpace.World,
    int Layer = RenderLayers.Debug) : IRenderCommand;
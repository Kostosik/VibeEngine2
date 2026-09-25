using Engine.Core.Math;
using Engine.Graphics.Debug;

namespace Engine.Graphics.Commands;

public sealed record DrawDebugCircleCommand(
    Vector2 Center,
    float Radius,
    DebugColor Color,
    bool Filled = false,
    int Segments = 16,
    DebugRenderSpace Space = DebugRenderSpace.World,
    int Layer = 1000) : IRenderCommand;
using Engine.Core.Math;

namespace Engine.Tooling.DebugVisualization;

public readonly record struct DebugRectangle(
    FixedBounds2 Bounds,
    DebugColor Color,
    int Layer = 0,
    bool Filled = false);
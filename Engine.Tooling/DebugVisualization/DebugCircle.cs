using Engine.Core.Math;

namespace Engine.Tooling.DebugVisualization;

public readonly record struct DebugCircle(
    FixedVector2 Center,
    Fixed32 Radius,
    DebugColor Color,
    int Layer = 0,
    bool Filled = false);
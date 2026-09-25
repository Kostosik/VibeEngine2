using Engine.Core.Math;

namespace Engine.Tooling.DebugVisualization;

public readonly record struct DebugLine(
    FixedVector2 Start,
    FixedVector2 End,
    DebugColor Color,
    int Layer = 0);
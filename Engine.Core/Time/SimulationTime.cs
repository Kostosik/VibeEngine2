using Engine.Core.Math;

namespace Engine.Core.Time;

public readonly record struct SimulationTime(
    Fixed32 Delta,
    Tick Tick);
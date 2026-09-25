using Engine.Core.Time;

namespace Engine.Core.Systems;

public readonly record struct FixedSystemContext(
    SimulationTime Time);
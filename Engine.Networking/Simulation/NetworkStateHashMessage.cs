using Engine.Core.Determinism;
using Engine.Core.Time;

namespace Engine.Networking.Simulation;

public readonly record struct NetworkStateHashMessage(
    Tick Tick,
    DeterministicStateHash Hash);
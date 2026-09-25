using Engine.Core.Math;

namespace Engine.Tests.Simulations;

public sealed class DeterministicSimulationState
{
    public FixedVector2 Position { get; set; }

    public FixedVector2 Velocity { get; set; }
}
using Engine.Core.Determinism;
using Engine.Core.Math;

namespace Engine.Simulations;

public readonly record struct SimulationVelocity(
    FixedVector2 Value)
    : IDeterministicState
{
    public void AddToHash(
        ref DeterministicStateHasher hasher)
    {
        hasher.AddFixedVector2(
            Value);
    }
}
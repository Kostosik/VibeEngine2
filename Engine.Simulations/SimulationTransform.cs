using Engine.Core.Determinism;
using Engine.Core.Math;

namespace Engine.Simulations;

public readonly record struct SimulationTransform(
    FixedVector2 Position)
    : IDeterministicState
{
    public void AddToHash(
        ref DeterministicStateHasher hasher)
    {
        hasher.AddFixedVector2(
            Position);
    }
}
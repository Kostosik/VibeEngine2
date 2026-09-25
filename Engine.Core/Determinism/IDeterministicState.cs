namespace Engine.Core.Determinism;

public interface IDeterministicState
{
    void AddToHash(
        ref DeterministicStateHasher hasher);
}
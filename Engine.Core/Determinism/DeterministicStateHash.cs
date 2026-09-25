namespace Engine.Core.Determinism;

public readonly record struct DeterministicStateHash(
    ulong Value)
{
    public static DeterministicStateHash Empty =>
        new(14695981039346656037UL);
}
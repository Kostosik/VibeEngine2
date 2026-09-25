namespace Engine.ECS.Persistence;

public sealed class EntityStoreState
{
    public EntityStoreState(
        uint nextIndex,
        uint[] generations,
        uint[] activeIndices,
        uint[] freeIndices)
    {
        if (nextIndex == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextIndex));
        }

        ArgumentNullException.ThrowIfNull(
            generations);

        ArgumentNullException.ThrowIfNull(
            activeIndices);

        ArgumentNullException.ThrowIfNull(
            freeIndices);

        NextIndex =
            nextIndex;

        Generations =
            generations.ToArray();

        ActiveIndices =
            activeIndices.ToArray();

        FreeIndices =
            freeIndices.ToArray();
    }

    public uint NextIndex { get; }

    public uint[] Generations { get; }

    public uint[] ActiveIndices { get; }

    public uint[] FreeIndices { get; }
}
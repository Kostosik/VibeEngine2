namespace Engine.ECS.Entities;

internal sealed class EntityStoreSnapshot
{
    public EntityStoreSnapshot(
        uint nextIndex,
        Dictionary<uint, uint> generations,
        HashSet<uint> activeEntities)
        : this(
            nextIndex,
            generations,
            activeEntities,
            Array.Empty<uint>())
    {
    }

    public EntityStoreSnapshot(
        uint nextIndex,
        Dictionary<uint, uint> generations,
        HashSet<uint> activeEntities,
        uint[] freeIndices)
    {
        NextIndex =
            nextIndex;

        Generations =
            generations;

        ActiveEntities =
            activeEntities;

        FreeIndices =
            freeIndices;
    }

    public uint NextIndex { get; }

    public Dictionary<uint, uint> Generations { get; }

    public HashSet<uint> ActiveEntities { get; }

    public uint[] FreeIndices { get; }
}
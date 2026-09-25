using Engine.ECS.Components;
using Engine.ECS.Entities;

namespace Engine.ECS;

public sealed class WorldSnapshot
{
    private readonly World _owner;

    internal WorldSnapshot(
        World owner,
        EntityStoreSnapshot entities,
        IReadOnlyList<IComponentStorageSnapshot> componentStorages)
    {
        _owner = owner;
        Entities = entities;
        ComponentStorages = componentStorages;
    }

    internal EntityStoreSnapshot Entities { get; }

    internal IReadOnlyList<IComponentStorageSnapshot>
        ComponentStorages
    { get; }

    internal bool BelongsTo(
        World world)
    {
        return ReferenceEquals(
            _owner,
            world);
    }
}
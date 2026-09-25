using Engine.ECS.Entities;

namespace Engine.ECS.Components;

internal sealed class ComponentStorageSnapshot<T>
    : IComponentStorageSnapshot
    where T : struct
{
    public ComponentStorageSnapshot(
        EntityId[] entities,
        T[] components)
    {
        Entities =
            entities;

        Components =
            components;
    }

    public Type ComponentType =>
        typeof(T);

    public EntityId[] Entities { get; }

    public T[] Components { get; }
}
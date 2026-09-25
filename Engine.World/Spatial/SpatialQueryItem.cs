using Engine.ECS.Entities;

namespace Engine.Worlds.Spatial;

public readonly ref struct SpatialQueryItem<T>
    where T : struct
{
    private readonly ref T _component;

    internal SpatialQueryItem(
        EntityId entity,
        ref T component)
    {
        Entity = entity;
        _component = ref component;
    }

    public EntityId Entity { get; }

    public ref T Component =>
        ref _component;
}
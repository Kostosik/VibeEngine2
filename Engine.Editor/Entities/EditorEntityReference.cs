using Engine.ECS.Entities;
using Engine.Worlds;

namespace Engine.Editor.Entities;

public sealed class EditorEntityReference
{
    internal EditorEntityReference(
        World world,
        EntityId entity)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        World = world;
        SetEntity(entity);
    }

    internal World World { get; }

    public EntityId Entity { get; private set; }

    public bool IsAlive =>
        Entity.IsValid &&
        World.EcsWorld.Exists(
            Entity);

    internal void EnsureWorld(
        World world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        if (!ReferenceEquals(
                World,
                world))
        {
            throw new InvalidOperationException(
                "Editor entity reference belongs to another World.");
        }
    }

    internal void SetEntity(
        EntityId entity)
    {
        Entity = entity;
    }
}
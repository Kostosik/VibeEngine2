using Engine.ECS.Entities;

namespace Engine.Tooling.Inspection;

public sealed class EntityInspection
{
    public EntityInspection(
        EntityId entity,
        IReadOnlyList<ComponentInspection> components)
    {
        Entity = entity;
        Components = components;
    }

    public EntityId Entity { get; }

    public IReadOnlyList<ComponentInspection> Components { get; }
}
using Engine.ECS.Entities;

namespace Engine.ECS.Inspection;

public interface IWorldInspector
{
    IReadOnlyList<EntityId> GetEntities();

    IReadOnlyList<Type> GetComponentTypes(
        EntityId entity);

    bool TryGetComponent(
        EntityId entity,
        Type componentType,
        out object? component);
}
using Engine.ECS.Entities;

namespace Engine.ECS.Inspection;

internal sealed class WorldInspector :
    IWorldInspector
{
    private readonly World _world;

    public WorldInspector(
        World world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        _world = world;
    }

    public IReadOnlyList<EntityId> GetEntities()
    {
        return _world.GetEntitiesForInspection();
    }

    public IReadOnlyList<Type> GetComponentTypes(
        EntityId entity)
    {
        return _world.GetComponentTypesForInspection(
            entity);
    }

    public bool TryGetComponent(
        EntityId entity,
        Type componentType,
        out object? component)
    {
        ArgumentNullException.ThrowIfNull(
            componentType);

        return _world.TryGetComponentForInspection(
            entity,
            componentType,
            out component);
    }
}
using Engine.ECS.Entities;
using Engine.ECS.Inspection;

namespace Engine.Tooling.Inspection;

public sealed class WorldInspectionService
{
    private readonly IWorldInspector _world;

    public WorldInspectionService(
        IWorldInspector world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        _world = world;
    }

    public IReadOnlyList<EntityId> GetEntities()
    {
        return _world.GetEntities();
    }

    public EntityInspection InspectEntity(
        EntityId entity)
    {
        var componentTypes =
            _world.GetComponentTypes(
                entity);

        var components =
            new List<ComponentInspection>(
                componentTypes.Count);

        foreach (var componentType in componentTypes)
        {
            if (!_world.TryGetComponent(
                    entity,
                    componentType,
                    out var component))
            {
                continue;
            }

            components.Add(
                new ComponentInspection(
                    componentType,
                    component!));
        }

        return new EntityInspection(
            entity,
            components);
    }
}
using Engine.ECS.Entities;
using Engine.ECS.Inspection;

namespace Engine.Tooling.Inspection;

public sealed class WorldInspectionService
{
    private readonly Func<IWorldInspector?> _worldProvider;

    public WorldInspectionService(
        IWorldInspector world)
    {
        ArgumentNullException.ThrowIfNull(world);

        _worldProvider = () => world;
    }

    public WorldInspectionService(
        Func<IWorldInspector?> worldProvider)
    {
        ArgumentNullException.ThrowIfNull(worldProvider);

        _worldProvider = worldProvider;
    }

    private IWorldInspector GetWorld()
    {
        return _worldProvider()
            ?? throw new InvalidOperationException(
                "No active world document. " +
                "Open or create a world before running this command.");
    }

    public IReadOnlyList<EntityId> GetEntities()
    {
        return GetWorld().GetEntities();
    }

    public EntityInspection InspectEntity(
        EntityId entity)
    {
        var world = GetWorld();

        var componentTypes =
            world.GetComponentTypes(entity);

        var components =
            new List<ComponentInspection>(
                componentTypes.Count);

        foreach (var componentType in componentTypes)
        {
            if (!world.TryGetComponent(
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
using Engine.ECS;
using Engine.ECS.Entities;
using Engine.ECS.Inspection;
using Engine.Editor.Selection;
using Engine.Worlds;

namespace Engine.Editor.Inspection;

public sealed class EditorInspector
{
    private readonly IWorldInspector _worldInspector;
    private readonly IEditorPropertyProviderRegistry _propertyProviders;
    private readonly Engine.Worlds.World _world;
    public EditorInspector(
        Engine.Worlds.World world,
        IEditorPropertyProviderRegistry propertyProviders)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(propertyProviders);
        _world = world;
        _worldInspector =
            world.EcsWorld.Inspector;

        _propertyProviders =
            propertyProviders;
    }

    public bool TryGetSelectedEntity(
        SelectionSet<EntityId> selection,
        out EntityId entity)
    {
        ArgumentNullException.ThrowIfNull(
            selection);

        if (selection.Count != 1)
        {
            entity = EntityId.Invalid;
            return false;
        }

        entity =
            selection.Items.First();

        return _worldInspector
            .GetEntities()
            .Contains(entity);
    }

    public IReadOnlyList<Type> GetComponentTypes(
        EntityId entity)
    {
        return _worldInspector.GetComponentTypes(
            entity);
    }

    public bool TryGetComponent(
        EntityId entity,
        Type componentType,
        out object? component)
    {
        return _worldInspector.TryGetComponent(
            entity,
            componentType,
            out component);
    }

    public IReadOnlyList<EditorProperty> GetProperties(
    EntityId entity,
    Type componentType)
    {
        ArgumentNullException.ThrowIfNull(
            componentType);

        if (!_worldInspector.TryGetComponent(
                entity,
                componentType,
                out _))
        {
            throw new InvalidOperationException(
                $"Entity '{entity}' does not contain component '{componentType.Name}'.");
        }

        if (!_propertyProviders.TryGetProvider(
                componentType,
                out var provider))
        {
            return Array.Empty<EditorProperty>();
        }

        return provider.GetProperties(
            new EcsComponentEditorTarget(
                _world,
                entity,
                componentType));
    }
}
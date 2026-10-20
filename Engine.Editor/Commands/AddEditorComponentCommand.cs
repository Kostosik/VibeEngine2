using Engine.ECS.Components;
using Engine.Editor.Components;
using Engine.Editor.Entities;
using Engine.Editor.Inspection;
using Engine.Transform;
using Engine.Worlds;

namespace Engine.Editor.Commands;

public sealed class AddEditorComponentCommand : IEditorCommand
{
    private readonly World _world;
    private readonly EditorEntityReference _reference;
    private readonly Type _componentType;
    private readonly object _defaultValue;
    private readonly EditorComponentDependencyRegistry _dependencies;
    public AddEditorComponentCommand(
        World world,
        EditorEntityReference reference,
        Type componentType,
        EditorComponentDependencyRegistry? dependencies = null,
        EditorComponentTypeRegistry? componentTypes = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(componentType);

        reference.EnsureWorld(
    world);

        if (!reference.IsAlive)
        {
            throw new ArgumentException(
                "Editor entity reference is not alive.",
                nameof(reference));
        }

        if (!componentType.IsValueType ||
            componentType.IsPrimitive ||
            componentType.IsEnum)
        {
            throw new ArgumentException(
                "ECS component type must be a non-primitive struct.",
                nameof(componentType));
        }
        _dependencies =
    dependencies ??
    EditorComponentDependencyRegistry.CreateDefault();
        _world = world;
        _reference = reference;
        _componentType = componentType;

        object defaultValue =
            componentTypes is not null
                ? componentTypes.CreateDefaultValue(componentType)
                : Activator.CreateInstance(componentType)
                  ?? throw new InvalidOperationException(
                      $"Could not create default value for " +
                      $"'{componentType.Name}'.");

        if (componentType == typeof(LocalTransform2D) &&
            world.EcsWorld.Has<WorldTransform2D>(
                reference.Entity))
        {
            var worldTransform =
                world.EcsWorld.Get<WorldTransform2D>(
                    reference.Entity);

            defaultValue =
                new LocalTransform2D(
                    new FixedTransform2D(
                        worldTransform.Position)
                    {
                        Rotation = worldTransform.Rotation,
                        Scale = worldTransform.Scale
                    });
        }

        _defaultValue = defaultValue;
    }

    public void Execute()
    {
        if (!ReferenceEntityExists())
        {
            throw new InvalidOperationException(
                "Editor entity reference is not alive.");
        }

        if (_componentType ==
    typeof(Engine.Worlds.Spatial.WorldPositionComponent))
        {
            throw new InvalidOperationException(
                "WorldPositionComponent must be managed by SpatialEntityManager.");
        }

        var existingComponentTypes =
    _world.EcsWorld.Inspector
        .GetComponentTypes(
            _reference.Entity)
        .ToHashSet();

        var missingDependencies =
            _dependencies.GetMissingDependencies(
                _componentType,
                existingComponentTypes);

        if (missingDependencies.Count > 0)
        {
            var missingNames =
                string.Join(
                    ", ",
                    missingDependencies.Select(
                        static type => type.Name));

            throw new InvalidOperationException(
                $"Cannot add '{_componentType.Name}'. " +
                $"Missing required component(s): {missingNames}.");
        }

        EcsComponentAccessor.Add(
            _world,
            _reference.Entity,
            _componentType,
            _defaultValue);
    }

    public void Undo()
    {
        EcsComponentAccessor.Remove(
            _world,
            _reference.Entity,
            _componentType);
    }

    private bool ReferenceEntityExists()
    {
        return _reference.IsAlive &&
               _world.EcsWorld.Exists(
                   _reference.Entity);
    }
}
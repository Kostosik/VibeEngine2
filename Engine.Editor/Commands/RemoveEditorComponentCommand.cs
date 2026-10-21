using Engine.Editor.Components;
using Engine.Editor.Entities;
using Engine.Editor.Inspection;
using Engine.Worlds;

namespace Engine.Editor.Commands;

public sealed class RemoveEditorComponentCommand : IEditorCommand
{
    private readonly World _world;
    private readonly EditorEntityReference _reference;
    private readonly Type _componentType;
    private readonly EditorComponentDependencyRegistry
    _dependencies;
    private object? _oldValue;
    private bool _captured;

    public RemoveEditorComponentCommand(
        World world,
        EditorEntityReference reference,
        Type componentType,
        EditorComponentDependencyRegistry? dependencies = null)
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

        _world = world;
        _reference = reference;
        _componentType = componentType;

        _dependencies =
    dependencies ??
    EditorComponentDependencyRegistry.CreateDefault();
    }

    public void Execute()
    {
        if (_componentType ==
            typeof(Engine.Worlds.Spatial.WorldPositionComponent))
        {
            throw new InvalidOperationException(
                "WorldPositionComponent must be managed by SpatialEntityManager.");
        }

        var existingComponentTypes =
    _world.EcsWorld.Inspector
        .GetComponentTypes(_reference.Entity)
        .ToHashSet();

        var dependents =
            _dependencies.GetDependents(
                _componentType,
                existingComponentTypes);

        if (dependents.Count > 0)
        {
            var dependentNames =
                string.Join(
                    ", ",
                    dependents.Select(
                        static type => type.Name));

            throw new InvalidOperationException(
                $"Cannot remove '{_componentType.Name}'. " +
                $"It is required by: {dependentNames}.");
        }

        CaptureOnce();

        EcsComponentAccessor.Remove(
            _world,
            _reference.Entity,
            _componentType);
    }

    public void Undo()
    {
        if (!_captured)
        {
            throw new InvalidOperationException(
                "Component state has not been captured.");
        }

        EcsComponentAccessor.Add(
            _world,
            _reference.Entity,
            _componentType,
            _oldValue!);
    }

    private void CaptureOnce()
    {
        if (_captured)
        {
            return;
        }

        if (!_world.EcsWorld.Inspector.TryGetComponent(
                _reference.Entity,
                _componentType,
                out var value) ||
            value is null)
        {
            throw new InvalidOperationException(
                $"Failed to capture component '{_componentType.Name}'.");
        }

        _oldValue = value;
        _captured = true;
    }
}
using Engine.Editor.Entities;
using Engine.Editor.Inspection;
using Engine.Worlds;

namespace Engine.Editor.Commands;

public sealed class AddEditorComponentCommand : IEditorCommand
{
    private readonly World _world;
    private readonly EditorEntityReference _reference;
    private readonly Type _componentType;
    private readonly object _defaultValue;

    public AddEditorComponentCommand(
        World world,
        EditorEntityReference reference,
        Type componentType)
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

        _defaultValue =
            Activator.CreateInstance(
                componentType)
            ?? throw new InvalidOperationException(
                $"Could not create default value for '{componentType.Name}'.");
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
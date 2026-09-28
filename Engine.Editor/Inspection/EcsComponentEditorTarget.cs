using Engine.ECS.Entities;
using Engine.Editor.Entities;
using Engine.Worlds;

namespace Engine.Editor.Inspection;

public sealed class EcsComponentEditorTarget
{
    public EcsComponentEditorTarget(
        World world,
        EditorEntityReference entity,
        Type componentType)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(componentType);

        if (!entity.IsAlive)
        {
            throw new ArgumentException(
                "Editor entity reference is not alive.",
                nameof(entity));
        }

        if (!componentType.IsValueType ||
            componentType.IsPrimitive ||
            componentType.IsEnum)
        {
            throw new ArgumentException(
                "ECS component type must be a non-primitive struct.",
                nameof(componentType));
        }

        World = world;
        EntityReference = entity;
        ComponentType = componentType;
    }

    public EcsComponentEditorTarget(
        World world,
        EntityId entity,
        Type componentType)
        : this(
            world,
            new EditorEntityReference(entity),
            componentType)
    {
    }

    public World World { get; }

    public EditorEntityReference EntityReference { get; }

    public EntityId Entity =>
        EntityReference.Entity;

    public Type ComponentType { get; }
}
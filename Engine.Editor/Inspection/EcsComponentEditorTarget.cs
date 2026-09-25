using Engine.ECS.Entities;
using Engine.Worlds;

namespace Engine.Editor.Inspection;

public sealed class EcsComponentEditorTarget
{
    public EcsComponentEditorTarget(
        World world,
        EntityId entity,
        Type componentType)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        ArgumentNullException.ThrowIfNull(
            componentType);

        if (!componentType.IsValueType ||
            componentType.IsPrimitive ||
            componentType.IsEnum)
        {
            throw new ArgumentException(
                "ECS component type must be a non-primitive struct.",
                nameof(componentType));
        }

        World = world;
        Entity = entity;
        ComponentType = componentType;
    }

    public World World { get; }

    public EntityId Entity { get; }

    public Type ComponentType { get; }
}
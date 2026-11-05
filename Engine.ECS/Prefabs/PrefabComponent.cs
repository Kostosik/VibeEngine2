using Engine.ECS.Entities;

namespace Engine.ECS.Prefabs;

public abstract class PrefabComponent
{
    public abstract Type ComponentType { get; }

    public abstract object BoxedValue { get; }

    public abstract PrefabComponent RemapEntityReferences(
        Func<EntityId, EntityId> remap);

    public abstract void AddToWorld(
        Engine.ECS.World world,
        EntityId entity);

    public static PrefabComponent Create(
        Type componentType,
        object value)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        ArgumentNullException.ThrowIfNull(value);

        if (!componentType.IsValueType ||
            componentType.IsEnum ||
            value.GetType() != componentType)
        {
            throw new ArgumentException(
                "Prefab components must contain a value of the registered struct type.",
                nameof(value));
        }

        var wrapperType =
            typeof(PrefabComponent<>).MakeGenericType(componentType);

        return (PrefabComponent)(
            Activator.CreateInstance(
                wrapperType,
                new object[] { value })
            ?? throw new InvalidOperationException(
                $"Could not create prefab component '{componentType.FullName}'."));
    }
}

public sealed class PrefabComponent<T> : PrefabComponent
    where T : struct
{
    public PrefabComponent(T component)
    {
        Component = component;
    }

    public T Component { get; }

    public override Type ComponentType => typeof(T);

    public override object BoxedValue => Component;

    public override void AddToWorld(
    Engine.ECS.World world,
    EntityId entity)
    {
        ArgumentNullException.ThrowIfNull(world);

        world.Add(entity, Component);
    }

    public override PrefabComponent RemapEntityReferences(
        Func<EntityId, EntityId> remap)
    {
        ArgumentNullException.ThrowIfNull(remap);

        if (Component is IPrefabEntityReferenceRemappable remappable)
        {
            var result =
                remappable.RemapPrefabEntityReferences(remap);

            if (result is not T typedComponent)
            {
                throw new InvalidOperationException(
                    $"Prefab remapper for '{typeof(T).FullName}' " +
                    "returned an incompatible component type.");
            }

            return new PrefabComponent<T>(typedComponent);
        }

        return new PrefabComponent<T>(Component);
    }
}
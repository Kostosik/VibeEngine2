using Engine.ECS.Entities;
using Engine.Worlds;

namespace Engine.Editor.Inspection;

internal static class EcsComponentAccessor
{
    public static object Get(
        World world,
        EntityId entity,
        Type componentType)
    {
        return InvokeGeneric(
            nameof(GetGeneric),
            world,
            entity,
            componentType,
            null);
    }

    public static void Set(
        World world,
        EntityId entity,
        Type componentType,
        object value)
    {
        ArgumentNullException.ThrowIfNull(
            value);

        InvokeGeneric(
            nameof(SetGeneric),
            world,
            entity,
            componentType,
            value);
    }

    private static object GetGeneric<T>(
        World world,
        EntityId entity)
        where T : struct
    {
        return world.EcsWorld
            .Get<T>(entity);
    }

    private static object SetGeneric<T>(
        World world,
        EntityId entity,
        object value)
        where T : struct
    {
        if (value is not T typedValue)
        {
            throw new ArgumentException(
                $"Value must be of type '{typeof(T)}'.",
                nameof(value));
        }

        world.EcsWorld
            .Get<T>(entity) = typedValue;

        return null!;
    }

    private static object InvokeGeneric(
        string methodName,
        World world,
        EntityId entity,
        Type componentType,
        object? value)
    {
        var method =
            typeof(EcsComponentAccessor)
                .GetMethod(
                    methodName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Static)
            ?? throw new InvalidOperationException(
                $"Accessor method '{methodName}' was not found.");

        var genericMethod =
            method.MakeGenericMethod(
                componentType);

        return genericMethod.Invoke(
                   null,
                   value is null
                       ? new object[] { world, entity }
                       : new object[] { world, entity, value })
               ?? null!;
    }
}
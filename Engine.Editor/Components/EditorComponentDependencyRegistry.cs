using Engine.Audio.Components;
using Engine.ECS.Components;
using Engine.Graphics.Tilemaps;
using Engine.Graphics2D.Sprites;
using Engine.Particles;

namespace Engine.Editor.Components;

public sealed class EditorComponentDependencyRegistry
{
    private readonly Dictionary<Type, HashSet<Type>> _requirements =
        new();

    public static EditorComponentDependencyRegistry CreateDefault()
    {
        var registry =
            new EditorComponentDependencyRegistry();
        registry.Register(
    typeof(TilemapComponent),
    typeof(WorldTransform2D));

        registry.Register(
            typeof(LocalTransform2D),
            typeof(WorldTransform2D));

        registry.Register(
            typeof(SpriteComponent),
            typeof(WorldTransform2D));

        registry.Register(
    typeof(AudioSourceComponent),
    typeof(WorldTransform2D));

        registry.Register(
            typeof(AudioListenerComponent),
            typeof(WorldTransform2D));

        registry.Register(
    typeof(ParticleEffectComponent),
    typeof(WorldTransform2D));

        return registry;
    }

    public void Register(
        Type componentType,
        params Type[] requiredComponentTypes)
    {
        ValidateComponentType(
            componentType,
            nameof(componentType));

        ArgumentNullException.ThrowIfNull(
            requiredComponentTypes);

        if (requiredComponentTypes.Length == 0)
        {
            throw new ArgumentException(
                "At least one required component must be specified.",
                nameof(requiredComponentTypes));
        }

        if (!_requirements.TryGetValue(
                componentType,
                out var requirements))
        {
            requirements = new HashSet<Type>();

            _requirements.Add(
                componentType,
                requirements);
        }

        foreach (var requiredType in requiredComponentTypes)
        {
            ValidateComponentType(
                requiredType,
                nameof(requiredComponentTypes));

            if (requiredType == componentType)
            {
                throw new ArgumentException(
                    "A component cannot require itself.",
                    nameof(requiredComponentTypes));
            }

            requirements.Add(requiredType);
        }
    }

    public IReadOnlyList<Type> GetMissingDependencies(
        Type componentType,
        IReadOnlySet<Type> existingComponentTypes)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        ArgumentNullException.ThrowIfNull(existingComponentTypes);

        if (!_requirements.TryGetValue(
                componentType,
                out var requirements))
        {
            return Array.Empty<Type>();
        }

        return requirements
            .Where(
                requiredType =>
                    !existingComponentTypes.Contains(
                        requiredType))
            .OrderBy(
                static type => type.FullName ?? type.Name,
                StringComparer.Ordinal)
            .ToArray();
    }

    public bool CanAdd(
        Type componentType,
        IReadOnlySet<Type> existingComponentTypes)
    {
        return GetMissingDependencies(
            componentType,
            existingComponentTypes).Count == 0;
    }

    public IReadOnlyList<Type> GetDependents(
    Type componentType,
    IReadOnlySet<Type> existingComponentTypes)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        ArgumentNullException.ThrowIfNull(existingComponentTypes);

        return _requirements
            .Where(entry =>
                existingComponentTypes.Contains(entry.Key) &&
                entry.Value.Contains(componentType))
            .Select(entry => entry.Key)
            .OrderBy(
                static type => type.FullName ?? type.Name,
                StringComparer.Ordinal)
            .ToArray();
    }

    private static void ValidateComponentType(
        Type type,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!type.IsValueType ||
            type.IsPrimitive ||
            type.IsEnum)
        {
            throw new ArgumentException(
                "ECS component type must be a non-primitive struct.",
                parameterName);
        }
    }
}

using Engine.Audio.Components;
using Engine.ECS.Components;
using Engine.Graphics.Tilemaps;
using Engine.Graphics2D.Sprites;
using Engine.Particles;

namespace Engine.Editor.Inspection;

public sealed class EditorComponentTypeRegistry
{
    private readonly HashSet<Type> _types = new();

    private readonly Dictionary<Type, Func<object>> _factories =
        new();

    public IReadOnlyCollection<Type> Types =>
        _types;

    public void Register<T>()
        where T : struct
    {
        Register(typeof(T));
    }

    public void Register<T>(
        Func<T> factory)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(factory);

        Register(
            typeof(T),
            () => factory());
    }

    public void Register(
        Type componentType)
    {
        ValidateComponentType(componentType);

        _types.Add(componentType);

        _factories.TryAdd(
            componentType,
            () =>
                Activator.CreateInstance(componentType)
                ?? throw new InvalidOperationException(
                    $"Could not create default value for " +
                    $"'{componentType.FullName}'."));
    }

    public void Register(
        Type componentType,
        Func<object> factory)
    {
        ValidateComponentType(componentType);

        ArgumentNullException.ThrowIfNull(factory);

        _types.Add(componentType);
        _factories[componentType] = factory;
    }

    public object CreateDefaultValue(
        Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        if (!_types.Contains(componentType))
        {
            throw new InvalidOperationException(
                $"Component type '{componentType.FullName}' " +
                "is not registered.");
        }

        if (!_factories.TryGetValue(
                componentType,
                out var factory))
        {
            throw new InvalidOperationException(
                $"No factory is registered for " +
                $"'{componentType.FullName}'.");
        }

        var value = factory();

        if (value is null ||
            value.GetType() != componentType)
        {
            throw new InvalidOperationException(
                $"The factory for '{componentType.FullName}' " +
                "returned an invalid component value.");
        }

        return value;
    }

    public void RegisterEngineComponents()
    {
        Register<TilemapComponent>(
    () => new TilemapComponent());
        Register<LocalTransform2D>();
        Register<WorldTransform2D>();
        Register<SpriteComponent>(
    () => new SpriteComponent());

        Register<AudioSourceComponent>(
    () => new AudioSourceComponent());

        Register<AudioListenerComponent>(
            () => new AudioListenerComponent());
        Register<ParticleEffectComponent>(
    () => new ParticleEffectComponent());
    }

    public bool Unregister(
        Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        _factories.Remove(componentType);

        return _types.Remove(componentType);
    }

    public bool Contains(
        Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        return _types.Contains(componentType);
    }

    private static void ValidateComponentType(
        Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        if (!componentType.IsValueType ||
            componentType.IsPrimitive ||
            componentType.IsEnum)
        {
            throw new ArgumentException(
                "ECS component type must be a non-primitive struct.",
                nameof(componentType));
        }
    }
}

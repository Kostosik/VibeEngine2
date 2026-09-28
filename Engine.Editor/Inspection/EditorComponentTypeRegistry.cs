namespace Engine.Editor.Inspection;

public sealed class EditorComponentTypeRegistry
{
    private readonly HashSet<Type> _types = new();

    public IReadOnlyCollection<Type> Types =>
        _types;

    public void Register<T>()
        where T : struct
    {
        Register(typeof(T));
    }

    public void Register(
        Type componentType)
    {
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

        _types.Add(
            componentType);
    }

    public bool Unregister(
        Type componentType)
    {
        ArgumentNullException.ThrowIfNull(
            componentType);

        return _types.Remove(
            componentType);
    }

    public bool Contains(
        Type componentType)
    {
        ArgumentNullException.ThrowIfNull(
            componentType);

        return _types.Contains(
            componentType);
    }
}
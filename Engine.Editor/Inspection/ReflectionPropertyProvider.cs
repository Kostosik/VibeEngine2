using System.Reflection;

namespace Engine.Editor.Inspection;

public sealed class ReflectionPropertyProvider :
    IEditorPropertyProvider
{
    public int Priority =>
        0;

    public bool CanInspect(
        Type type)
    {
        ArgumentNullException.ThrowIfNull(
            type);

        return type
            .GetProperties(
                BindingFlags.Instance |
                BindingFlags.Public)
            .Any(property =>
                property.GetMethod is not null);
    }

    public IReadOnlyList<EditorProperty> GetProperties(
        object target)
    {
        ArgumentNullException.ThrowIfNull(
            target);

        var properties =
            target
                .GetType()
                .GetProperties(
                    BindingFlags.Instance |
                    BindingFlags.Public)
                .Where(property =>
                    property.GetMethod is not null &&
                    property.GetIndexParameters().Length == 0)
                .OrderBy(
                    property =>
                        property
                            .GetCustomAttribute<EditorPropertyAttribute>()
                            ?.Order ?? 0)
                .ThenBy(
                    property => property.Name,
                    StringComparer.Ordinal)
                .Select(
                    property =>
                        CreateProperty(
                            target,
                            property))
                .ToArray();

        return properties;
    }

    private static EditorProperty CreateProperty(
        object target,
        PropertyInfo property)
    {
        Action<object?>? setter = null;

        if (property.SetMethod is not null)
        {
            setter =
                value =>
                {
                    property.SetValue(
                        target,
                        value);
                };
        }

        var metadata =
            property.GetCustomAttribute<
                EditorPropertyAttribute>();

        return new EditorProperty(
            property.Name,
            property.PropertyType,
            () =>
                property.GetValue(target),
            setter,
            metadata?.DisplayName,
            metadata?.Order ?? 0);
    }
}
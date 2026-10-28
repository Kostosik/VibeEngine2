using Engine.Core.Diagnostics;
using Engine.Tooling.Debugging;
using System.Reflection;

namespace Engine.Editor.Inspection;

public sealed class EcsComponentPropertyProvider :
    IEditorPropertyProvider
{
    public int Priority =>
        100;

    public bool CanInspect(
        Type type)
    {
        ArgumentNullException.ThrowIfNull(
            type);

        return type.IsValueType &&
               !type.IsPrimitive &&
               !type.IsEnum;
    }

    private DebugConsole? _console;

    private readonly HashSet<(Type ComponentType, string PropertyName)>
        _reportedUnsupportedProperties = new();

    public EcsComponentPropertyProvider(
        DebugConsole? console = null)
    {
        _console = console;
    }

    internal void SetConsole(DebugConsole? console)
    {
        _console = console;
    }

    public IReadOnlyList<EditorProperty> GetProperties(
        object target)
    {
        if (target is not EcsComponentEditorTarget component)
        {
            throw new ArgumentException(
                "Target must be an EcsComponentEditorTarget.",
                nameof(target));
        }

        return component.ComponentType
            .GetProperties(
                BindingFlags.Instance |
                BindingFlags.Public)
                .Where(property =>
                {
                    var getter = property.GetMethod;

                    if (getter is null ||
                        property.GetIndexParameters().Length != 0)
                    {
                        return false;
                    }

                    var returnType = getter.ReturnType;

                    var supported =
                        !returnType.IsByRef &&
                        !returnType.IsByRefLike &&
                        !property.PropertyType.IsByRefLike;

                    if (supported ||
                        _console is null ||
                        !_reportedUnsupportedProperties.Add(
                            (component.ComponentType, property.Name)))
                    {
                        return supported;
                    }

                    _console.Write(
                        DiagnosticLevel.Debug,
                        $"Inspector skipped property " +
                        $"'{component.ComponentType.FullName}.{property.Name}'. " +
                        $"Return type: '{returnType}'.",
                        "INSPECTOR_UNSUPPORTED_PROPERTY");

                    return false;
                })
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
                        component,
                        property))
            .ToArray();
    }

    private static EditorProperty CreateProperty(
        EcsComponentEditorTarget target,
        PropertyInfo property)
    {
        Action<object?>? setter = null;

        if (property.SetMethod is { IsPublic: true })
        {
            setter =
                value =>
                {
                    var component =
                        EcsComponentAccessor.Get(
                            target.World,
                            target.Entity,
                            target.ComponentType);

                    property.SetValue(
                        component,
                        value);

                    EcsComponentAccessor.Set(
                        target.World,
                        target.Entity,
                        target.ComponentType,
                        component);
                };
        }

        var metadata =
            property.GetCustomAttribute<
                EditorPropertyAttribute>();

        return new EditorProperty(
            property.Name,
            property.PropertyType,
            () =>
            {
                var component =
                    EcsComponentAccessor.Get(
                        target.World,
                        target.Entity,
                        target.ComponentType);

                return property.GetValue(
                    component);
            },
            setter,
            metadata?.DisplayName,
            metadata?.Order ?? 0);
    }
}
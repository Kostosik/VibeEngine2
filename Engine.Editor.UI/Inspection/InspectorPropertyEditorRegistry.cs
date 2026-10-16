using Engine.Editor.Documents;
using Engine.Editor.Inspection;
using Engine.UI.Controls;
using Engine.UI.Core;

namespace Engine.Editor.UI.Inspection;

public sealed class InspectorPropertyEditorRegistry
{
    private readonly List<IInspectorPropertyEditor> _editors =
        new();

    public IReadOnlyList<IInspectorPropertyEditor> Editors =>
        _editors;

    public static InspectorPropertyEditorRegistry CreateDefault()
    {
        var registry =
            new InspectorPropertyEditorRegistry();

        registry.Register(
            new BooleanInspectorPropertyEditor());

        registry.Register(
            new ScalarInspectorPropertyEditor());

        registry.Register(
            new EnumInspectorPropertyEditor());

        registry.Register(
            new ReadOnlyInspectorPropertyEditor());

        registry.Register(
    new Fixed32InspectorPropertyEditor());

        registry.Register(
            new FixedVector2InspectorPropertyEditor());

        return registry;
    }

    public void Register(
        IInspectorPropertyEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        if (_editors.Contains(editor))
        {
            throw new InvalidOperationException(
                "Inspector property editor is already registered.");
        }

        _editors.Add(editor);
    }

    public bool Unregister(
        IInspectorPropertyEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        return _editors.Remove(editor);
    }

    public bool TryGetEditor(
        Type propertyType,
        out IInspectorPropertyEditor? editor)
    {
        ArgumentNullException.ThrowIfNull(propertyType);

        propertyType =
            Nullable.GetUnderlyingType(propertyType)
            ?? propertyType;

        editor = null;

        foreach (var candidate in _editors)
        {
            if (!candidate.CanEdit(propertyType))
            {
                continue;
            }

            if (editor is null ||
                candidate.Priority >= editor.Priority)
            {
                editor = candidate;
            }
        }

        return editor is not null;
    }

    public UiWidget Create(
        EditorDocument document,
        EditorProperty property)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(property);

        if (!TryGetEditor(
                property.PropertyType,
                out var editor) ||
            editor is null)
        {
            throw new InvalidOperationException(
                $"No Inspector editor is registered for '{property.PropertyType.FullName}'.");
        }

        return editor.Create(
            document,
            property);
    }
}
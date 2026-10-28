using Engine.Editor.Documents;
using Engine.Editor.Inspection;

namespace Engine.Editor.Commands;

public sealed class SetEditorPropertyCommand :
    IEditorCommand
{
    private readonly EditorProperty _property;
    private readonly object? _newValue;
    private readonly EditorDocument? _document;

    private object? _oldValue;
    private bool _hasOldValue;

    public SetEditorPropertyCommand(
        EditorProperty property,
        object? newValue,
        EditorDocument? document = null)
    {
        ArgumentNullException.ThrowIfNull(property);

        if (property.IsReadOnly)
        {
            throw new InvalidOperationException(
                $"Property '{property.Name}' is read-only.");
        }

        if (property.AffectsTransformHierarchy &&
            document is null)
        {
            throw new ArgumentNullException(
                nameof(document),
                "Editing transform properties requires an EditorDocument.");
        }

        _property = property;
        _newValue = newValue;
        _document = document;
    }

    public void Execute()
    {
        if (!_hasOldValue)
        {
            _oldValue = _property.GetValue();
            _hasOldValue = true;
        }

        _property.SetValue(_newValue);

        RefreshTransformsIfNeeded();
    }

    public void Undo()
    {
        if (!_hasOldValue)
        {
            throw new InvalidOperationException(
                "Editor command has not been executed.");
        }

        _property.SetValue(_oldValue);

        RefreshTransformsIfNeeded();
    }

    private void RefreshTransformsIfNeeded()
    {
        if (_property.AffectsTransformHierarchy)
        {
            _document!.RefreshTransforms();
        }
    }
}
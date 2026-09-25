using Engine.Editor.Inspection;

namespace Engine.Editor.Commands;

public sealed class SetEditorPropertyCommand :
    IEditorCommand
{
    private readonly EditorProperty _property;
    private readonly object? _newValue;

    private object? _oldValue;
    private bool _hasOldValue;

    public SetEditorPropertyCommand(
        EditorProperty property,
        object? newValue)
    {
        ArgumentNullException.ThrowIfNull(
            property);

        if (property.IsReadOnly)
        {
            throw new InvalidOperationException(
                $"Property '{property.Name}' is read-only.");
        }

        _property = property;
        _newValue = newValue;
    }

    public void Execute()
    {
        if (!_hasOldValue)
        {
            _oldValue =
                _property.GetValue();

            _hasOldValue = true;
        }

        _property.SetValue(
            _newValue);
    }

    public void Undo()
    {
        if (!_hasOldValue)
        {
            throw new InvalidOperationException(
                "Editor command has not been executed.");
        }

        _property.SetValue(
            _oldValue);
    }
}
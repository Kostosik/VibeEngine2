namespace Engine.Editor.Commands;

public sealed class EditorCommandHistory
{
    private readonly List<IEditorCommand> _commands = new();

    private int _position;
    private int _savedPosition;

    public bool CanUndo =>
        _position > 0;

    public bool CanRedo =>
        _position < _commands.Count;

    public bool IsDirty =>
        _position != _savedPosition;

    public int UndoCount =>
        _position;

    public int RedoCount =>
        _commands.Count - _position;

    public void Execute(
        IEditorCommand command)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        if (_position < _commands.Count)
        {
            _commands.RemoveRange(
                _position,
                _commands.Count - _position);
        }

        command.Execute();

        _commands.Add(
            command);

        _position++;
    }

    public void Undo()
    {
        if (!CanUndo)
        {
            return;
        }

        var command =
            _commands[_position - 1];

        command.Undo();

        _position--;
    }

    public void Redo()
    {
        if (!CanRedo)
        {
            return;
        }

        var command =
            _commands[_position];

        command.Execute();

        _position++;
    }

    public void MarkSaved()
    {
        _savedPosition =
            _position;
    }

    public void Clear()
    {
        _commands.Clear();

        _position = 0;
        _savedPosition = 0;
    }
}
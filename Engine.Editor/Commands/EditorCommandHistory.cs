namespace Engine.Editor.Commands;

public sealed class EditorCommandHistory
{
    private readonly List<IEditorCommand> _commands = new();

    private int _position;
    private int _savedPosition;

    public event Action? Changed;

    public bool CanUndo =>
        _position > 0;

    public bool CanRedo =>
        _position < _commands.Count;

    public bool IsDirty =>
        _savedPosition != _position;

    public int UndoCount =>
        _position;

    public int RedoCount =>
        _commands.Count - _position;

    public void Execute(
        IEditorCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // The command must succeed before the redo branch
        // is discarded. Otherwise a failed command would
        // corrupt the history.
        command.Execute();

        if (_position < _commands.Count)
        {
            _commands.RemoveRange(
                _position,
                _commands.Count - _position);

            // The saved state belonged to the discarded
            // branch and can no longer be identified by
            // position alone.
            _savedPosition = -1;
        }

        _commands.Add(
            command);

        _position++;

        Changed?.Invoke();
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

        Changed?.Invoke();
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

        Changed?.Invoke();
    }

    public void MarkSaved()
    {
        _savedPosition =
            _position;

        Changed?.Invoke();
    }

    public void Clear()
    {
        _commands.Clear();

        _position = 0;
        _savedPosition = 0;

        Changed?.Invoke();
    }
}
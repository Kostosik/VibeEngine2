namespace Engine.Editor.Commands;

public interface IEditorCommand
{
    void Execute();

    void Undo();
}
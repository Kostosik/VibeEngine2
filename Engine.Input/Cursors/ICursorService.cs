namespace Engine.Input.Cursors;

public interface ICursorService
{
    CursorShape Current { get; }

    void Set(
        CursorShape shape);
}
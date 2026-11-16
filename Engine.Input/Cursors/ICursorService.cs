using Engine.Input.Cursors;

public interface ICursorService
{
    CursorShape Current { get; }

    PointerMode Mode { get; }

    bool IsModeSupported(PointerMode mode);

    void Set(CursorShape shape);

    void SetMode(PointerMode mode);
}
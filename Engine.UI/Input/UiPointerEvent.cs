using Engine.Core.Math;
using Engine.Input;

namespace Engine.UI.Input;

public sealed class UiPointerEvent
{
    public UiPointerEvent(
        Vector2 position,
        float scrollDelta = 0.0f)
        : this(
            position,
            InputMouseButton.Left,
            scrollDelta)
    {
    }

    public UiPointerEvent(
        Vector2 position,
        InputMouseButton button,
        float scrollDelta = 0.0f)
    {
        Position = position;
        Button = button;
        ScrollDelta = scrollDelta;
    }

    public Vector2 Position { get; }

    public InputMouseButton Button { get; }

    public float ScrollDelta { get; }

    public bool Handled { get; set; }

    public bool CaptureRequested { get; internal set; }

    public bool ReleaseCaptureRequested { get; internal set; }

    public void RequestCapture()
    {
        CaptureRequested = true;
    }

    public void ReleaseCapture()
    {
        ReleaseCaptureRequested = true;
    }
}
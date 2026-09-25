using Engine.Core.Math;

namespace Engine.Editor.Viewport;

public sealed class EditorViewport
{
    public EditorViewport()
    {
        State =
            new EditorViewportState();

        Transform =
            new EditorViewportTransform(
                State);
    }

    public EditorViewportState State { get; }

    public EditorViewportTransform Transform { get; }

    public void Pan(
        Vector2 offset)
    {
        State.Pan(
            offset);
    }

    public void Zoom(
        float zoom)
    {
        State.SetZoom(
            zoom);
    }
}
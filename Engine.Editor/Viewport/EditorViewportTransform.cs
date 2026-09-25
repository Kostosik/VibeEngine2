using Engine.Core.Math;

namespace Engine.Editor.Viewport;

public sealed class EditorViewportTransform
{
    private readonly EditorViewportState _state;

    public EditorViewportTransform(
        EditorViewportState state)
    {
        ArgumentNullException.ThrowIfNull(
            state);

        _state = state;
    }

    public Vector2 WorldToScreen(
        Vector2 worldPosition)
    {
        var relative =
            worldPosition -
            _state.Center;

        return new Vector2(
            _state.Size.X * 0.5f +
            relative.X * _state.Zoom,
            _state.Size.Y * 0.5f -
            relative.Y * _state.Zoom);
    }

    public Vector2 ScreenToWorld(
        Vector2 screenPosition)
    {
        var relative =
            new Vector2(
                screenPosition.X -
                _state.Size.X * 0.5f,
                _state.Size.Y * 0.5f -
                screenPosition.Y);

        return _state.Center +
               relative / _state.Zoom;
    }
}
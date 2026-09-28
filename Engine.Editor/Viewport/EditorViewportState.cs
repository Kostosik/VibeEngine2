using Engine.Core.Math;

namespace Engine.Editor.Viewport;

public sealed class EditorViewportState
{
    private Vector2 _size =
        Vector2.Zero;

    private Vector2 _center =
        Vector2.Zero;

    private float _zoom = 1.0f;

    public Vector2 Size =>
        _size;

    public Vector2 Center =>
        _center;

    public float Zoom =>
        _zoom;

    public void SetSize(
        Vector2 size)
    {
        if (!float.IsFinite(size.X) ||
            !float.IsFinite(size.Y) ||
            size.X < 0.0f ||
            size.Y < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size));
        }

        _size = size;
    }

    public void SetCenter(
        Vector2 center)
    {
        if (!float.IsFinite(center.X) ||
            !float.IsFinite(center.Y))
        {
            throw new ArgumentOutOfRangeException(
                nameof(center));
        }

        _center = center;
    }

    public void SetZoom(
        float zoom)
    {
        if (zoom <= 0.0f ||
            !float.IsFinite(zoom))
        {
            throw new ArgumentOutOfRangeException(
                nameof(zoom));
        }

        _zoom = zoom;
    }

    public void Pan(
        Vector2 offset)
    {
        if (!float.IsFinite(offset.X) ||
            !float.IsFinite(offset.Y))
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset));
        }

        var center =
            _center + offset;

        if (!float.IsFinite(center.X) ||
            !float.IsFinite(center.Y))
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset));
        }

        _center = center;
    }
}
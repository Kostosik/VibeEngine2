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
        if (size.X < 0.0f ||
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
        _center = center;
    }

    public void SetZoom(
        float zoom)
    {
        if (zoom <= 0.0f ||
            float.IsNaN(zoom) ||
            float.IsInfinity(zoom))
        {
            throw new ArgumentOutOfRangeException(
                nameof(zoom));
        }

        _zoom = zoom;
    }

    public void Pan(
        Vector2 offset)
    {
        _center += offset;
    }
}
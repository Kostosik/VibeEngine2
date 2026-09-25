using Engine.Core.Math;

namespace Engine.Graphics.Cameras;

public sealed class Camera
{
    private Vector2 _position;
    private float _zoom = 1.0f;
    private Vector2 _viewportSize;

    public Camera(
        Vector2 viewportSize)
    {
        SetViewportSize(
            viewportSize);
    }

    public Engine.Core.Math.Rectangle WorldBounds
    {
        get
        {
            var width =
                _viewportSize.X / _zoom;

            var height =
                _viewportSize.Y / _zoom;

            return new Engine.Core.Math.Rectangle(
                _position.X - width * 0.5f,
                _position.Y - height * 0.5f,
                width,
                height);
        }
    }
    public Vector2 Position
    {
        get => _position;
        set => _position = value;
    }

    public float Zoom
    {
        get => _zoom;
        set
        {
            if (value <= 0.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Zoom must be greater than zero.");
            }

            _zoom = value;
        }
    }

    public Vector2 ViewportSize =>
        _viewportSize;

    public Vector2 WorldToScreen(
        Vector2 worldPosition)
    {
        var offset =
            worldPosition - _position;

        return new Vector2(
            offset.X * _zoom +
            _viewportSize.X * 0.5f,

            offset.Y * _zoom +
            _viewportSize.Y * 0.5f);
    }

    public Vector2 ScreenToWorld(
        Vector2 screenPosition)
    {
        return new Vector2(
            (screenPosition.X -
             _viewportSize.X * 0.5f) /
            _zoom +
            _position.X,

            (screenPosition.Y -
             _viewportSize.Y * 0.5f) /
            _zoom +
            _position.Y);
    }

    public void SetViewportSize(
        Vector2 viewportSize)
    {
        if (viewportSize.X <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(viewportSize),
                "Viewport width must be greater than zero.");
        }

        if (viewportSize.Y <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(viewportSize),
                "Viewport height must be greater than zero.");
        }

        _viewportSize =
            viewportSize;
    }
}
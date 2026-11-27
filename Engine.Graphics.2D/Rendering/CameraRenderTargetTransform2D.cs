using Engine.Camera;
using Engine.Core.Math;

namespace Engine.Graphics2D.Rendering;

/// <summary>
/// Converts camera screen coordinates into render target coordinates.
/// Camera screen coordinates use a top-left origin.
/// Render target coordinates use the same top-left origin,
/// unless explicitly converted to bottom-left coordinates.
/// </summary>
public readonly struct CameraRenderTargetTransform2D
{
    private readonly Camera2D _camera;
    private readonly int _targetWidth;
    private readonly int _targetHeight;

    public CameraRenderTargetTransform2D(
        Camera2D camera,
        int targetWidth,
        int targetHeight)
    {
        ArgumentNullException.ThrowIfNull(camera);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            targetWidth);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            targetHeight);

        _camera = camera;
        _targetWidth = targetWidth;
        _targetHeight = targetHeight;
    }

    /// <summary>
    /// Scale from camera viewport coordinates
    /// to render target coordinates.
    /// </summary>
    public Vector2 Scale
    {
        get
        {
            var viewport = _camera.ViewportSize;

            return new Vector2(
                _targetWidth / viewport.X,
                _targetHeight / viewport.Y);
        }
    }

    /// <summary>
    /// Converts camera screen coordinates
    /// into render target coordinates.
    /// Both coordinate systems use top-left origin.
    /// </summary>
    public Vector2 CameraScreenToTarget(
        Vector2 screenPosition)
    {
        return screenPosition * Scale;
    }

    /// <summary>
    /// Transforms a rotated camera-space quad into render target coordinates.
    /// Rotation is applied before non-uniform target scaling.
    /// </summary>
    public CameraRenderTargetQuad2D CameraScreenQuadToTarget(
        Vector2 position,
        Vector2 size,
        float rotationDegrees)
    {
        var right = position.X + size.X;
        var bottom = position.Y + size.Y;

        var origin = new Vector2(
            position.X + size.X * 0.5f,
            position.Y + size.Y * 0.5f);

        var topLeft = RotatePoint(
            new Vector2(position.X, position.Y),
            origin,
            rotationDegrees);

        var topRight = RotatePoint(
            new Vector2(right, position.Y),
            origin,
            rotationDegrees);

        var bottomRight = RotatePoint(
            new Vector2(right, bottom),
            origin,
            rotationDegrees);

        var bottomLeft = RotatePoint(
            new Vector2(position.X, bottom),
            origin,
            rotationDegrees);

        return new CameraRenderTargetQuad2D(
            CameraScreenToTarget(topLeft),
            CameraScreenToTarget(topRight),
            CameraScreenToTarget(bottomRight),
            CameraScreenToTarget(bottomLeft));
    }

    private static Vector2 RotatePoint(
        Vector2 point,
        Vector2 origin,
        float rotationDegrees)
    {
        if (MathF.Abs(rotationDegrees) < 0.0001f)
            return point;

        var radians = rotationDegrees * (MathF.PI / 180.0f);
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);

        var dx = point.X - origin.X;
        var dy = point.Y - origin.Y;

        return new Vector2(
            origin.X + dx * cos - dy * sin,
            origin.Y + dx * sin + dy * cos);
    }


    /// <summary>
    /// Converts camera screen coordinates
    /// into render target coordinates with bottom-left origin.
    /// </summary>
    public Vector2 CameraScreenToTargetBottomLeft(
        Vector2 screenPosition)
    {
        var targetPosition =
            CameraScreenToTarget(screenPosition);

        return new Vector2(
            targetPosition.X,
            _targetHeight - targetPosition.Y);
    }

    /// <summary>
    /// Converts world position into render target coordinates.
    /// Uses top-left origin.
    /// </summary>
    public Vector2 WorldToTarget(
        Vector2 worldPosition)
    {
        return CameraScreenToTarget(
            _camera.WorldToScreen(worldPosition));
    }

    /// <summary>
    /// Converts world position into render target coordinates.
    /// Uses bottom-left origin.
    /// </summary>
    public Vector2 WorldToTargetBottomLeft(
        Vector2 worldPosition)
    {
        return CameraScreenToTargetBottomLeft(
            _camera.WorldToScreen(worldPosition));
    }

    /// <summary>
    /// Converts world dimensions into render target dimensions.
    /// Applies camera zoom and independent X/Y scaling.
    /// </summary>
    public Vector2 WorldSizeToTarget(
        Vector2 worldSize)
    {
        var zoomedSize = worldSize * _camera.Zoom;
        var scale = Scale;

        return new Vector2(
            zoomedSize.X * scale.X,
            zoomedSize.Y * scale.Y);
    }
}
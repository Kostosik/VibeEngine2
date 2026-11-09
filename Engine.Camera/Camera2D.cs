
using Engine.Core.Math;

namespace Engine.Camera;

public sealed class Camera2D
{
    private Vector2 _position;
    private Vector2 _viewportSize;
    private Vector2 _followVelocity;
    private float _zoom = 1.0f;
    private Vector2 _deadZoneSize;

    private Rectangle? _worldLimit;
    public Rectangle? WorldLimit => _worldLimit;
    public Camera2D(Vector2 viewportSize)
    {
        SetViewportSize(viewportSize);
    }

    /// <summary>
    /// Dead zone size in screen pixels, centered in the viewport.
    /// A zero dimension makes the camera follow the target exactly on that axis.
    /// </summary>
    public Vector2 DeadZoneSize
    {
        get => _deadZoneSize;
        set
        {
            if (!float.IsFinite(value.X) ||
                !float.IsFinite(value.Y) ||
                value.X < 0.0f ||
                value.Y < 0.0f ||
                value.X > _viewportSize.X ||
                value.Y > _viewportSize.Y)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Dead zone dimensions must be finite, non-negative and fit inside the viewport.");
            }

            _deadZoneSize = value;
        }
    }

    public Vector2 Position
    {
        get => _position;
        set
        {
            ValidateFiniteVector(value, nameof(value));
            ValidateWorldBounds(value, _zoom, _viewportSize, nameof(value));

            var position = ClampToWorldLimit(
                value,
                _zoom,
                _viewportSize,
                _worldLimit);

            ValidateWorldBounds(
                position,
                _zoom,
                _viewportSize,
                nameof(value));

            _position = position;
            _followVelocity = Vector2.Zero;
        }
    }

    public float Zoom
    {
        get => _zoom;
        set
        {
            if (!float.IsFinite(value) || value <= 0.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Zoom must be finite and greater than zero.");
            }

            ValidateWorldBounds(_position, value, _viewportSize, nameof(value));
            var position = ClampToWorldLimit(
                    _position,
                    value,
                    _viewportSize,
                    _worldLimit);

            ValidateWorldBounds(
                position,
                value,
                _viewportSize,
                nameof(value));

            _zoom = value;
            _position = position;
            _followVelocity = Vector2.Zero;
        }
    }

    public Vector2 ViewportSize => _viewportSize;

    public Rectangle WorldBounds
    {
        get
        {
            var width = _viewportSize.X / _zoom;
            var height = _viewportSize.Y / _zoom;

            return new Rectangle(
                _position.X - width * 0.5f,
                _position.Y - height * 0.5f,
                width,
                height);
        }
    }

    public bool IsVisible(Rectangle bounds)
    {
        var view = WorldBounds;

        return
            bounds.X < view.X + view.Width &&
            bounds.X + bounds.Width > view.X &&
            bounds.Y < view.Y + view.Height &&
            bounds.Y + bounds.Height > view.Y;
    }

    public Vector2 WorldToScreen(Vector2 worldPosition)
    {
        var offset = worldPosition - _position;

        return new Vector2(
            offset.X * _zoom + _viewportSize.X * 0.5f,
            offset.Y * _zoom + _viewportSize.Y * 0.5f);
    }

    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        return new Vector2(
            (screenPosition.X - _viewportSize.X * 0.5f) / _zoom + _position.X,
            (screenPosition.Y - _viewportSize.Y * 0.5f) / _zoom + _position.Y);
    }

    public void SetViewportSize(Vector2 viewportSize)
    {
        if (!float.IsFinite(viewportSize.X) || viewportSize.X <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(viewportSize),
                "Viewport width must be finite and greater than zero.");
        }

        if (!float.IsFinite(viewportSize.Y) || viewportSize.Y <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(viewportSize),
                "Viewport height must be finite and greater than zero.");
        }

        ValidateWorldBounds(
            _position,
            _zoom,
            viewportSize,
            nameof(viewportSize));

        var position = ClampToWorldLimit(
            _position,
            _zoom,
            viewportSize,
            _worldLimit);

        ValidateWorldBounds(
            position,
            _zoom,
            viewportSize,
            nameof(viewportSize));
        _deadZoneSize = new Vector2(
            Math.Min(_deadZoneSize.X, viewportSize.X),
            Math.Min(_deadZoneSize.Y, viewportSize.Y));
        _viewportSize = viewportSize;
        _position = position;
        _followVelocity = Vector2.Zero;
    }

    public void SetWorldLimit(Rectangle bounds)
    {
        if (!float.IsFinite(bounds.X) ||
            !float.IsFinite(bounds.Y) ||
            !float.IsFinite(bounds.Width) ||
            !float.IsFinite(bounds.Height) ||
            bounds.Width <= 0.0f ||
            bounds.Height <= 0.0f ||
            !float.IsFinite(bounds.X + bounds.Width) ||
            !float.IsFinite(bounds.Y + bounds.Height))
        {
            throw new ArgumentOutOfRangeException(
                nameof(bounds),
                "World limit must have finite coordinates and positive dimensions.");
        }

        var position = ClampToWorldLimit(
            _position,
            _zoom,
            _viewportSize,
            bounds);

        ValidateWorldBounds(
            position,
            _zoom,
            _viewportSize,
            nameof(bounds));

        _worldLimit = bounds;
        _position = position;
        _followVelocity = Vector2.Zero;
    }

    private static Vector2 ClampToWorldLimit(
    Vector2 position,
    float zoom,
    Vector2 viewportSize,
    Rectangle? worldLimit)
    {
        if (worldLimit is not Rectangle bounds)
            return position;

        var halfWidth = viewportSize.X / zoom * 0.5f;
        var halfHeight = viewportSize.Y / zoom * 0.5f;

        var minX = bounds.X + halfWidth;
        var maxX = bounds.X + bounds.Width - halfWidth;

        var minY = bounds.Y + halfHeight;
        var maxY = bounds.Y + bounds.Height - halfHeight;

        var x = minX > maxX
            ? bounds.X + bounds.Width * 0.5f
            : Math.Clamp(position.X, minX, maxX);

        var y = minY > maxY
            ? bounds.Y + bounds.Height * 0.5f
            : Math.Clamp(position.Y, minY, maxY);

        return new Vector2(x, y);
    }

    public void ClearWorldLimit()
    {
        _worldLimit = null;
        _followVelocity = Vector2.Zero;
    }

    /// <summary>
    /// Smoothly moves the camera toward a world-space target.
    /// Call this method every update while following the target.
    /// </summary>
    public void Follow(
        Vector2 targetPosition,
        float deltaSeconds,
        float smoothTime = 0.2f)
    {
        ValidateFiniteVector(targetPosition, nameof(targetPosition));

        if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        }

        if (!float.IsFinite(smoothTime) || smoothTime < 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(smoothTime));
        }
        var followTarget = GetFollowTarget(targetPosition);
        if (smoothTime == 0.0f)
        {
            _position = ClampToWorldLimit(
                followTarget,
                _zoom,
                _viewportSize,
                _worldLimit);

            ValidateWorldBounds(
                _position,
                _zoom,
                _viewportSize,
                nameof(followTarget));

            _followVelocity = Vector2.Zero;
            return;
        }

        if (deltaSeconds == 0.0f)
            return;

        var x = SmoothDampAxis(
            _position.X,
            followTarget.X,
            _followVelocity.X,
            smoothTime,
            deltaSeconds);

        var y = SmoothDampAxis(
            _position.Y,
            followTarget.Y,
            _followVelocity.Y,
            smoothTime,
            deltaSeconds);

        var nextPosition = new Vector2(x.Position, y.Position);
        var nextVelocity = new Vector2(x.Velocity, y.Velocity);

        var constrainedPosition = ClampToWorldLimit(
            nextPosition,
            _zoom,
            _viewportSize,
            _worldLimit);

        if (constrainedPosition.X != nextPosition.X)
            nextVelocity = new Vector2(0.0f, nextVelocity.Y);

        if (constrainedPosition.Y != nextPosition.Y)
            nextVelocity = new Vector2(nextVelocity.X, 0.0f);

        ValidateWorldBounds(
            constrainedPosition,
            _zoom,
            _viewportSize,
            nameof(followTarget));

        _position = constrainedPosition;
        _followVelocity = nextVelocity;
    }

    private Vector2 GetFollowTarget(Vector2 targetPosition)
    {
        var screenPosition = WorldToScreen(targetPosition);

        var centerX = _viewportSize.X * 0.5f;
        var centerY = _viewportSize.Y * 0.5f;

        var halfWidth = _deadZoneSize.X * 0.5f;
        var halfHeight = _deadZoneSize.Y * 0.5f;

        var left = centerX - halfWidth;
        var right = centerX + halfWidth;
        var top = centerY - halfHeight;
        var bottom = centerY + halfHeight;

        var correctionX = 0.0f;
        var correctionY = 0.0f;

        if (screenPosition.X < left)
            correctionX = (screenPosition.X - left) / _zoom;
        else if (screenPosition.X > right)
            correctionX = (screenPosition.X - right) / _zoom;

        if (screenPosition.Y < top)
            correctionY = (screenPosition.Y - top) / _zoom;
        else if (screenPosition.Y > bottom)
            correctionY = (screenPosition.Y - bottom) / _zoom;

        return new Vector2(
            _position.X + correctionX,
            _position.Y + correctionY);
    }

    /// <summary>
    /// Clears accumulated follow velocity.
    /// The camera keeps its current position.
    /// </summary>
    public void StopFollowing()
    {
        _followVelocity = Vector2.Zero;
    }

    private static (float Position, float Velocity) SmoothDampAxis(
        float current,
        float target,
        float velocity,
        float smoothTime,
        float deltaTime)
    {
        // Critically damped spring approximation.
        // Uses arithmetic only, without frame-rate-dependent fixed lerp.
        var omega = 2.0 / smoothTime;
        var x = omega * deltaTime;

        var exponential = 1.0 /
            (1.0 + x + 0.48 * x * x + 0.235 * x * x * x);

        var change = (double)current - target;
        var temporary = (velocity + omega * change) * deltaTime;

        var nextVelocity = (velocity - omega * temporary) * exponential;
        var output = target + (change + temporary) * exponential;

        // Prevent overshooting the target.
        if ((target - current > 0.0f && output > target) ||
            (target - current < 0.0f && output < target))
        {
            output = target;
            nextVelocity = 0.0;
        }

        // Avoid storing invalid float state after extreme input values.
        if (!double.IsFinite(output) ||
            !double.IsFinite(nextVelocity) ||
            Math.Abs(output) > float.MaxValue ||
            Math.Abs(nextVelocity) > float.MaxValue)
        {
            return (target, 0.0f);
        }

        return ((float)output, (float)nextVelocity);
    }

    private static void ValidateFiniteVector(
        Vector2 value,
        string parameterName)
    {
        if (!float.IsFinite(value.X) || !float.IsFinite(value.Y))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Vector components must be finite.");
        }
    }

    private static void ValidateWorldBounds(
        Vector2 position,
        float zoom,
        Vector2 viewportSize,
        string parameterName)
    {
        var width = viewportSize.X / zoom;
        var height = viewportSize.Y / zoom;

        var halfWidth = width * 0.5f;
        var halfHeight = height * 0.5f;

        var left = position.X - halfWidth;
        var right = position.X + halfWidth;
        var top = position.Y - halfHeight;
        var bottom = position.Y + halfHeight;

        if (!float.IsFinite(width) ||
            !float.IsFinite(height) ||
            !float.IsFinite(left) ||
            !float.IsFinite(right) ||
            !float.IsFinite(top) ||
            !float.IsFinite(bottom))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Camera configuration produces non-finite world bounds.");
        }
    }
}

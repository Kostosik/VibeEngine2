
using Engine.Core.Math;

namespace Engine.Camera;

public sealed class Camera3DController
{
    private const float MaxPitch =
        89.0f * MathF.PI / 180.0f;

    private const float MinFieldOfView =
        25.0f * MathF.PI / 180.0f;

    private const float MaxFieldOfView =
        90.0f * MathF.PI / 180.0f;

    private readonly Camera3D _camera;

    private float _yaw;
    private float _pitch;

    private float _moveSpeed = 5.0f;
    private float _lookSensitivity = 0.0025f;
    private float _zoomSensitivity = 0.05f;

    public Camera3D Camera => _camera;

    public float MoveSpeed
    {
        get => _moveSpeed;
        set
        {
            if (!float.IsFinite(value) || value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _moveSpeed = value;
        }
    }

    public float LookSensitivity
    {
        get => _lookSensitivity;
        set
        {
            if (!float.IsFinite(value) || value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _lookSensitivity = value;
        }
    }

    public float ZoomSensitivity
    {
        get => _zoomSensitivity;
        set
        {
            if (!float.IsFinite(value) || value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _zoomSensitivity = value;
        }
    }

    public Camera3DController(Camera3D camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        _camera = camera;

        var forward = camera.Forward;

        _yaw = MathF.Atan2(
            -forward.X,
            -forward.Z);

        _pitch = MathF.Asin(
            Math.Clamp(forward.Y, -1.0f, 1.0f));

        _pitch = Math.Clamp(
            _pitch,
            -MaxPitch,
            MaxPitch);
    }

    /// <summary>
    /// Moves relative to the camera orientation.
    /// X = right, Y = up, Z = forward.
    /// </summary>
    public void Move(
        Vector3 localDirection,
        float deltaSeconds)
    {
        if (!float.IsFinite(deltaSeconds) ||
            deltaSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(deltaSeconds));
        }

        ValidateFiniteVector(localDirection);

        if (deltaSeconds == 0 ||
            _moveSpeed == 0)
        {
            return;
        }

        var maxComponent = Math.Max(
            Math.Abs((double)localDirection.X),
            Math.Max(
                Math.Abs((double)localDirection.Y),
                Math.Abs((double)localDirection.Z)));

        if (maxComponent <= 0)
        {
            return;
        }

        var x = localDirection.X / maxComponent;
        var y = localDirection.Y / maxComponent;
        var z = localDirection.Z / maxComponent;

        var length = Math.Sqrt(
            x * x + y * y + z * z);

        x /= length;
        y /= length;
        z /= length;

        var worldDirection =
            _camera.Right * (float)x +
            _camera.Up * (float)y +
            _camera.Forward * (float)z;

        var distance = _moveSpeed * deltaSeconds;

        _camera.Position += worldDirection * distance;
    }

    /// <summary>
    /// Applies yaw and pitch changes in radians.
    /// </summary>
    public void Look(
        float yawDelta,
        float pitchDelta)
    {
        if (!float.IsFinite(yawDelta))
        {
            throw new ArgumentOutOfRangeException(nameof(yawDelta));
        }

        if (!float.IsFinite(pitchDelta))
        {
            throw new ArgumentOutOfRangeException(nameof(pitchDelta));
        }

        _yaw = (float)Math.IEEERemainder(
            (double)_yaw + yawDelta,
            Math.Tau);

        _pitch = Math.Clamp(
            _pitch + pitchDelta,
            -MaxPitch,
            MaxPitch);

        var yawRotation =
            Quaternion.CreateFromAxisAngle(
                Vector3.UnitY,
                _yaw);

        var pitchRotation =
            Quaternion.CreateFromAxisAngle(
                Vector3.UnitX,
                _pitch);

        _camera.Rotation =
            yawRotation * pitchRotation;
    }

    /// <summary>
    /// Converts mouse movement into camera rotation.
    /// </summary>
    public void LookFromMouseDelta(Vector2 delta)
    {
        if (!float.IsFinite(delta.X) ||
            !float.IsFinite(delta.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(delta));
        }

        Look(
            -delta.X * _lookSensitivity,
            -delta.Y * _lookSensitivity);
    }

    /// <summary>
    /// Positive scroll zooms in, negative scroll zooms out.
    /// </summary>
    public void Zoom(float scrollDelta)
    {
        if (!float.IsFinite(scrollDelta))
        {
            throw new ArgumentOutOfRangeException(nameof(scrollDelta));
        }

        var fieldOfView =
            _camera.FieldOfView -
            scrollDelta * _zoomSensitivity;

        _camera.FieldOfView = Math.Clamp(
            fieldOfView,
            MinFieldOfView,
            MaxFieldOfView);
    }

    private static void ValidateFiniteVector(Vector3 value)
    {
        if (!float.IsFinite(value.X) ||
            !float.IsFinite(value.Y) ||
            !float.IsFinite(value.Z))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Movement direction must be finite.");
        }
    }
}

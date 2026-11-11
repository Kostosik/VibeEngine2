
using Engine.Core.Math;

namespace Engine.Camera;

/// <summary>
/// A backend-independent 3D camera.
/// Uses a right-handed coordinate system with forward along -Z.
/// </summary>
public sealed class Camera3D
{
    private Vector3 _position = Vector3.Zero;
    private Quaternion _rotation = Quaternion.Identity;

    private float _fieldOfView = MathF.PI / 3.0f;
    private float _aspectRatio = 16.0f / 9.0f;
    private float _nearPlane = 0.1f;
    private float _farPlane = 1000.0f;

    public Vector3 Position
    {
        get => _position;
        set
        {
            ValidateFiniteVector(value, nameof(value));
            _position = value;
        }
    }

    public Quaternion Rotation
    {
        get => _rotation;
        set => _rotation = NormalizeQuaternion(value);
    }

    /// <summary>
    /// Vertical field of view in radians.
    /// </summary>
    public float FieldOfView
    {
        get => _fieldOfView;
        set
        {
            if (!float.IsFinite(value) ||
                value <= 0.0f ||
                value >= MathF.PI)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _fieldOfView = value;
        }
    }

    public float AspectRatio
    {
        get => _aspectRatio;
        set
        {
            if (!float.IsFinite(value) || value <= 0.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _aspectRatio = value;
        }
    }


    /// <summary>
    /// Updates the camera aspect ratio from viewport dimensions.
    /// </summary>
    public void SetViewportSize(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        AspectRatio = (float)width / height;
    }


    public float NearPlane
    {
        get => _nearPlane;
        set
        {
            if (!float.IsFinite(value) ||
                value <= 0.0f ||
                value >= _farPlane)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _nearPlane = value;
        }
    }

    public float FarPlane
    {
        get => _farPlane;
        set
        {
            if (!float.IsFinite(value) ||
                value <= _nearPlane)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _farPlane = value;
        }
    }

    public Vector3 Forward =>
        _rotation.Transform(-Vector3.UnitZ);

    public Vector3 Up =>
        _rotation.Transform(Vector3.UnitY);

    public Vector3 Right =>
        _rotation.Transform(Vector3.UnitX);

    public Matrix4x4 ViewMatrix =>
        Matrix4x4.CreateLookAt(
            _position,
            _position + Forward,
            Up);

    public Matrix4x4 ProjectionMatrix =>
        Matrix4x4.CreatePerspectiveFieldOfView(
            _fieldOfView,
            _aspectRatio,
            _nearPlane,
            _farPlane);

    /// <summary>
    /// Rotates the camera towards a target using world up.
    /// </summary>
    public void LookAt(Vector3 target)
    {
        LookAt(target, Vector3.UnitY);
    }

    /// <summary>
    /// Rotates the camera towards a target using a custom up direction.
    /// </summary>
    public void LookAt(
        Vector3 target,
        Vector3 up)
    {
        var forward = NormalizeDifference(
            target,
            _position,
            nameof(target));

        var referenceUp = NormalizeVector(
            up,
            nameof(up));

        var right = Vector3.Cross(
            forward,
            referenceUp);

        if (right.LengthSquared() <= 1e-12f)
        {
            throw new ArgumentException(
                "Camera up vector must not be parallel to the viewing direction.",
                nameof(up));
        }

        right = NormalizeVector(
            right,
            nameof(up));

        var correctedUp = Vector3.Cross(
            right,
            forward);

        var backward = -forward;

        _rotation = CreateQuaternionFromBasis(
            right,
            correctedUp,
            backward);
    }

    private static Quaternion CreateQuaternionFromBasis(
        Vector3 right,
        Vector3 up,
        Vector3 backward)
    {
        // Rotation matrix represented using basis vectors as columns.
        var m00 = right.X;
        var m01 = up.X;
        var m02 = backward.X;

        var m10 = right.Y;
        var m11 = up.Y;
        var m12 = backward.Y;

        var m20 = right.Z;
        var m21 = up.Z;
        var m22 = backward.Z;

        var trace = m00 + m11 + m22;

        float x;
        float y;
        float z;
        float w;

        if (trace > 0.0f)
        {
            var s = MathF.Sqrt(trace + 1.0f) * 2.0f;

            w = 0.25f * s;
            x = (m21 - m12) / s;
            y = (m02 - m20) / s;
            z = (m10 - m01) / s;
        }
        else if (m00 > m11 && m00 > m22)
        {
            var s = MathF.Sqrt(
                1.0f + m00 - m11 - m22) * 2.0f;

            w = (m21 - m12) / s;
            x = 0.25f * s;
            y = (m01 + m10) / s;
            z = (m02 + m20) / s;
        }
        else if (m11 > m22)
        {
            var s = MathF.Sqrt(
                1.0f + m11 - m00 - m22) * 2.0f;

            w = (m02 - m20) / s;
            x = (m01 + m10) / s;
            y = 0.25f * s;
            z = (m12 + m21) / s;
        }
        else
        {
            var s = MathF.Sqrt(
                1.0f + m22 - m00 - m11) * 2.0f;

            w = (m10 - m01) / s;
            x = (m02 + m20) / s;
            y = (m12 + m21) / s;
            z = 0.25f * s;
        }

        return NormalizeQuaternion(
            new Quaternion(x, y, z, w));
    }

    private static Quaternion NormalizeQuaternion(
        Quaternion value)
    {
        if (!float.IsFinite(value.X) ||
            !float.IsFinite(value.Y) ||
            !float.IsFinite(value.Z) ||
            !float.IsFinite(value.W))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Quaternion components must be finite.");
        }

        var max = Math.Max(
            Math.Max(Math.Abs((double)value.X), Math.Abs((double)value.Y)),
            Math.Max(Math.Abs((double)value.Z), Math.Abs((double)value.W)));

        if (max <= 0.0)
        {
            throw new ArgumentException(
                "Camera rotation must not be a zero quaternion.",
                nameof(value));
        }

        var x = value.X / max;
        var y = value.Y / max;
        var z = value.Z / max;
        var w = value.W / max;

        var length = Math.Sqrt(
            x * x + y * y + z * z + w * w);

        return new Quaternion(
            (float)(x / length),
            (float)(y / length),
            (float)(z / length),
            (float)(w / length));
    }

    private static Vector3 NormalizeVector(
        Vector3 value,
        string parameterName)
    {
        return NormalizeComponents(
            value.X,
            value.Y,
            value.Z,
            parameterName);
    }

    private static Vector3 NormalizeDifference(
        Vector3 value,
        Vector3 origin,
        string parameterName)
    {
        return NormalizeComponents(
            (double)value.X - origin.X,
            (double)value.Y - origin.Y,
            (double)value.Z - origin.Z,
            parameterName);
    }

    private static Vector3 NormalizeComponents(
        double x,
        double y,
        double z,
        string parameterName)
    {
        if (!double.IsFinite(x) ||
            !double.IsFinite(y) ||
            !double.IsFinite(z))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Vector components must be finite.");
        }

        var max = Math.Max(
            Math.Abs(x),
            Math.Max(Math.Abs(y), Math.Abs(z)));

        if (max <= 0.0)
        {
            throw new ArgumentException(
                "Cannot normalize a zero vector.",
                parameterName);
        }

        x /= max;
        y /= max;
        z /= max;

        var length = Math.Sqrt(
            x * x + y * y + z * z);

        return new Vector3(
            (float)(x / length),
            (float)(y / length),
            (float)(z / length));
    }

    private static void ValidateFiniteVector(
        Vector3 value,
        string parameterName)
    {
        if (!float.IsFinite(value.X) ||
            !float.IsFinite(value.Y) ||
            !float.IsFinite(value.Z))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Vector components must be finite.");
        }
    }
}

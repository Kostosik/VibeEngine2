namespace Engine.Core.Math;

public readonly record struct Matrix4x4(
    float M11, float M12, float M13, float M14,
    float M21, float M22, float M23, float M24,
    float M31, float M32, float M33, float M34,
    float M41, float M42, float M43, float M44)
{
    public static Matrix4x4 Identity => new(
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1);

    public bool IsIdentity => this == Identity;

    public Vector3 Translation =>
        new(M41, M42, M43);

    public static Matrix4x4 CreateTranslation(Vector3 position)
    {
        return new Matrix4x4(
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            position.X, position.Y, position.Z, 1);
    }

    public static Matrix4x4 CreateScale(Vector3 scale)
    {
        return new Matrix4x4(
            scale.X, 0, 0, 0,
            0, scale.Y, 0, 0,
            0, 0, scale.Z, 0,
            0, 0, 0, 1);
    }

    public static Matrix4x4 CreateScale(float scale)
    {
        return CreateScale(new Vector3(scale, scale, scale));
    }

    public static Matrix4x4 CreateRotation(Quaternion rotation)
    {
        var q = rotation.Normalize();

        var xx = q.X * q.X;
        var yy = q.Y * q.Y;
        var zz = q.Z * q.Z;

        var xy = q.X * q.Y;
        var xz = q.X * q.Z;
        var yz = q.Y * q.Z;

        var xw = q.X * q.W;
        var yw = q.Y * q.W;
        var zw = q.Z * q.W;

        return new Matrix4x4(
            1 - 2 * (yy + zz),
            2 * (xy + zw),
            2 * (xz - yw),
            0,

            2 * (xy - zw),
            1 - 2 * (xx + zz),
            2 * (yz + xw),
            0,

            2 * (xz + yw),
            2 * (yz - xw),
            1 - 2 * (xx + yy),
            0,

            0, 0, 0, 1);
    }


    /// <summary>
    /// Creates a right-handed view matrix for a camera
    /// looking from cameraPosition towards cameraTarget.
    ///
    /// Uses row-vector convention:
    /// world * view * projection.
    /// </summary>
    public static Matrix4x4 CreateLookAt(
        Vector3 cameraPosition,
        Vector3 cameraTarget,
        Vector3 cameraUpVector)
    {
        ValidateFiniteVector(
            cameraPosition,
            nameof(cameraPosition));

        ValidateFiniteVector(
            cameraTarget,
            nameof(cameraTarget));

        ValidateFiniteVector(
            cameraUpVector,
            nameof(cameraUpVector));

        var forward = NormalizeLookAtVector(
            (double)cameraTarget.X - cameraPosition.X,
            (double)cameraTarget.Y - cameraPosition.Y,
            (double)cameraTarget.Z - cameraPosition.Z,
            nameof(cameraTarget),
            "Camera position and target must not be equal.");

        var backward = -forward;

        var referenceUp = NormalizeLookAtVector(
            cameraUpVector.X,
            cameraUpVector.Y,
            cameraUpVector.Z,
            nameof(cameraUpVector),
            "Camera up vector must not be zero.");

        var right = Vector3.Cross(
            referenceUp,
            backward);

        if (right.LengthSquared() <= 1e-12f)
        {
            throw new ArgumentException(
                "Camera up vector must not be parallel to the viewing direction.",
                nameof(cameraUpVector));
        }

        right = NormalizeLookAtVector(
            right.X,
            right.Y,
            right.Z,
            nameof(cameraUpVector),
            "Cannot calculate camera right direction.");

        var up = Vector3.Cross(
            backward,
            right);

        return new Matrix4x4(
            right.X, up.X, backward.X, 0,
            right.Y, up.Y, backward.Y, 0,
            right.Z, up.Z, backward.Z, 0,

            GetNegatedDot(right, cameraPosition, nameof(cameraPosition)),
            GetNegatedDot(up, cameraPosition, nameof(cameraPosition)),
            GetNegatedDot(backward, cameraPosition, nameof(cameraPosition)),
            1);
    }

    private static Vector3 NormalizeLookAtVector(
        double x,
        double y,
        double z,
        string paramName,
        string errorMessage)
    {
        if (!double.IsFinite(x) ||
            !double.IsFinite(y) ||
            !double.IsFinite(z))
        {
            throw new ArgumentException(
                errorMessage,
                paramName);
        }

        var maxComponent = System.Math.Max(
            System.Math.Abs(x),
            System.Math.Max(
                System.Math.Abs(y),
                System.Math.Abs(z)));

        if (maxComponent <= 0.0)
        {
            throw new ArgumentException(
                errorMessage,
                paramName);
        }

        x /= maxComponent;
        y /= maxComponent;
        z /= maxComponent;

        var length = System.Math.Sqrt(
            x * x + y * y + z * z);

        return new Vector3(
            (float)(x / length),
            (float)(y / length),
            (float)(z / length));
    }

    private static float GetNegatedDot(
        Vector3 direction,
        Vector3 position,
        string paramName)
    {
        var value = -(
            (double)direction.X * position.X +
            (double)direction.Y * position.Y +
            (double)direction.Z * position.Z);

        var result = (float)value;

        if (!float.IsFinite(result))
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                "Camera position produces a non-finite view matrix.");
        }

        return result;
    }

    private static void ValidateFiniteVector(
        Vector3 value,
        string paramName)
    {
        if (!float.IsFinite(value.X) ||
            !float.IsFinite(value.Y) ||
            !float.IsFinite(value.Z))
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                "Vector components must be finite.");
        }
    }


    public static Matrix4x4 CreatePerspectiveFieldOfView(
        float fieldOfView,
        float aspectRatio,
        float nearPlane,
        float farPlane)
    {
        if (fieldOfView <= 0 || fieldOfView >= MathF.PI)
        {
            throw new ArgumentOutOfRangeException(nameof(fieldOfView));
        }

        if (aspectRatio <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(aspectRatio));
        }

        ValidateClipPlanes(nearPlane, farPlane);

        var yScale = 1.0f / MathF.Tan(fieldOfView * 0.5f);
        var xScale = yScale / aspectRatio;

        return new Matrix4x4(
            xScale, 0, 0, 0,
            0, yScale, 0, 0,
            0, 0,
            -(farPlane + nearPlane) / (farPlane - nearPlane),
            -1,
            0, 0,
            -(2.0f * farPlane * nearPlane) / (farPlane - nearPlane),
            0);
    }

    public static Matrix4x4 CreateOrthographic(
        float width,
        float height,
        float nearPlane,
        float farPlane)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        ValidateClipPlanes(nearPlane, farPlane);

        return new Matrix4x4(
            2.0f / width, 0, 0, 0,
            0, 2.0f / height, 0, 0,
            0, 0, -2.0f / (farPlane - nearPlane), 0,
            0, 0,
            -(farPlane + nearPlane) / (farPlane - nearPlane),
            1);
    }

    public Vector3 TransformPoint(Vector3 point)
    {
        var x =
            point.X * M11 +
            point.Y * M21 +
            point.Z * M31 +
            M41;

        var y =
            point.X * M12 +
            point.Y * M22 +
            point.Z * M32 +
            M42;

        var z =
            point.X * M13 +
            point.Y * M23 +
            point.Z * M33 +
            M43;

        var w =
            point.X * M14 +
            point.Y * M24 +
            point.Z * M34 +
            M44;

        if (MathF.Abs(w) <= 1e-7f)
        {
            throw new InvalidOperationException(
                "Cannot transform point with zero homogeneous coordinate.");
        }

        if (w != 1.0f)
        {
            x /= w;
            y /= w;
            z /= w;
        }

        return new Vector3(x, y, z);
    }

    public Vector3 TransformDirection(Vector3 direction)
    {
        return new Vector3(
            direction.X * M11 +
            direction.Y * M21 +
            direction.Z * M31,

            direction.X * M12 +
            direction.Y * M22 +
            direction.Z * M32,

            direction.X * M13 +
            direction.Y * M23 +
            direction.Z * M33);
    }

    private static void ValidateClipPlanes(
        float nearPlane,
        float farPlane)
    {
        if (nearPlane <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nearPlane));
        }

        if (farPlane <= nearPlane)
        {
            throw new ArgumentOutOfRangeException(nameof(farPlane));
        }
    }

    public static Matrix4x4 operator *(
        Matrix4x4 a,
        Matrix4x4 b)
    {
        return new Matrix4x4(
            a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.M41,
            a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.M42,
            a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.M43,
            a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44,

            a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.M41,
            a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.M42,
            a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.M43,
            a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44,

            a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.M41,
            a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.M42,
            a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.M43,
            a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44,

            a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + a.M44 * b.M41,
            a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + a.M44 * b.M42,
            a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + a.M44 * b.M43,
            a.M41 * b.M14 + a.M42 * b.M24 + a.M43 * b.M34 + a.M44 * b.M44);
    }
}

namespace Engine.Core.Math;

public readonly record struct Matrix3x2(
    float M11,
    float M12,
    float M21,
    float M22,
    float M31,
    float M32)
{
    public static Matrix3x2 Identity =>
        new(
            1, 0,
            0, 1,
            0, 0);

    public bool IsIdentity => this == Identity;

    public bool IsFinite =>
        float.IsFinite(M11) &&
        float.IsFinite(M12) &&
        float.IsFinite(M21) &&
        float.IsFinite(M22) &&
        float.IsFinite(M31) &&
        float.IsFinite(M32);

    public Vector2 Translation =>
        new(M31, M32);

    public float Determinant =>
        M11 * M22 - M12 * M21;

    public static Matrix3x2 CreateTranslation(
        Vector2 position)
    {
        return new Matrix3x2(
            1, 0,
            0, 1,
            position.X, position.Y);
    }

    public static Matrix3x2 CreateScale(Vector2 scale)
    {
        return new Matrix3x2(
            scale.X, 0,
            0, scale.Y,
            0, 0);
    }

    public static Matrix3x2 CreateScale(float scale)
    {
        return CreateScale(new Vector2(scale, scale));
    }

    /// <summary>
    /// Creates a counter-clockwise rotation matrix.
    /// Angle is specified in radians.
    /// Uses row-vector convention.
    /// </summary>
    public static Matrix3x2 CreateRotation(float radians)
    {
        var cosine = MathF.Cos(radians);
        var sine = MathF.Sin(radians);

        return new Matrix3x2(
            cosine, sine,
            -sine, cosine,
            0, 0);
    }

    public Vector2 TransformPoint(Vector2 point)
    {
        return new Vector2(
            point.X * M11 +
            point.Y * M21 +
            M31,

            point.X * M12 +
            point.Y * M22 +
            M32);
    }

    public Vector2 TransformDirection(Vector2 direction)
    {
        return new Vector2(
            direction.X * M11 +
            direction.Y * M21,

            direction.X * M12 +
            direction.Y * M22);
    }

    /// <summary>
    /// Attempts to calculate the inverse matrix.
    /// Returns false for singular or non-finite results.
    /// </summary>
    public bool TryInvert(out Matrix3x2 inverse)
    {
        inverse = default;

        var determinant = Determinant;

        if (!float.IsFinite(determinant) ||
            determinant == 0.0f)
        {
            return false;
        }

        var inverseDeterminant = 1.0f / determinant;

        var m11 = M22 * inverseDeterminant;
        var m12 = -M12 * inverseDeterminant;
        var m21 = -M21 * inverseDeterminant;
        var m22 = M11 * inverseDeterminant;

        var m31 = -(M31 * m11 + M32 * m21);
        var m32 = -(M31 * m12 + M32 * m22);

        var result = new Matrix3x2(
            m11, m12,
            m21, m22,
            m31, m32);

        if (!result.IsFinite)
            return false;

        inverse = result;
        return true;
    }

    public static Matrix3x2 operator *(
        Matrix3x2 a,
        Matrix3x2 b)
    {
        return new Matrix3x2(
            a.M11 * b.M11 + a.M12 * b.M21,
            a.M11 * b.M12 + a.M12 * b.M22,

            a.M21 * b.M11 + a.M22 * b.M21,
            a.M21 * b.M12 + a.M22 * b.M22,

            a.M31 * b.M11 + a.M32 * b.M21 + b.M31,
            a.M31 * b.M12 + a.M32 * b.M22 + b.M32);
    }
}

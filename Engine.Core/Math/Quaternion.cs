
namespace Engine.Core.Math;

public readonly record struct Quaternion(
    float X,
    float Y,
    float Z,
    float W)
{
    public static Quaternion Identity =>
        new(0.0f, 0.0f, 0.0f, 1.0f);

    public bool IsFinite =>
    float.IsFinite(X) &&
    float.IsFinite(Y) &&
    float.IsFinite(Z) &&
    float.IsFinite(W);

    public float LengthSquared()
    {
        return X * X + Y * Y + Z * Z + W * W;
    }

    public float Length()
    {
        return MathF.Sqrt(LengthSquared());
    }

    public Quaternion Normalize()
    {
        if (!IsFinite)
        {
            throw new InvalidOperationException(
                "Cannot normalize a non-finite quaternion.");
        }

        var maxComponent = MathF.Max(
            MathF.Abs(X),
            MathF.Max(
                MathF.Abs(Y),
                MathF.Max(MathF.Abs(Z), MathF.Abs(W))));

        if (maxComponent == 0.0f)
        {
            return Identity;
        }

        var x = (double)X / maxComponent;
        var y = (double)Y / maxComponent;
        var z = (double)Z / maxComponent;
        var w = (double)W / maxComponent;

        var length = System.Math.Sqrt(
            x * x + y * y + z * z + w * w);

        return new Quaternion(
            (float)(x / length),
            (float)(y / length),
            (float)(z / length),
            (float)(w / length));
    }

    public Quaternion Conjugate()
    {
        return new Quaternion(-X, -Y, -Z, W);
    }

    public Quaternion Inverse()
    {
        if (!IsFinite)
        {
            throw new InvalidOperationException(
                "Cannot invert a non-finite quaternion.");
        }

        var lengthSquared =
            (double)X * X +
            (double)Y * Y +
            (double)Z * Z +
            (double)W * W;

        if (lengthSquared == 0.0)
        {
            throw new InvalidOperationException(
                "Cannot invert a zero quaternion.");
        }

        var result = new Quaternion(
            (float)(-(double)X / lengthSquared),
            (float)(-(double)Y / lengthSquared),
            (float)(-(double)Z / lengthSquared),
            (float)((double)W / lengthSquared));

        if (!result.IsFinite)
        {
            throw new OverflowException(
                "Quaternion inverse cannot be represented as finite float values.");
        }

        return result;
    }

    public static float Dot(
        Quaternion left,
        Quaternion right)
    {
        return
            left.X * right.X +
            left.Y * right.Y +
            left.Z * right.Z +
            left.W * right.W;
    }

    public static Quaternion CreateFromAxisAngle(
        Vector3 axis,
        float angle)
    {
        var normalizedAxis = axis.Normalize();

        if (normalizedAxis == Vector3.Zero)
        {
            return Identity;
        }

        var halfAngle = angle * 0.5f;
        var sin = MathF.Sin(halfAngle);
        var cos = MathF.Cos(halfAngle);

        return new Quaternion(
            normalizedAxis.X * sin,
            normalizedAxis.Y * sin,
            normalizedAxis.Z * sin,
            cos);
    }

    public Vector3 Transform(Vector3 vector)
    {
        var rotation = Normalize();

        var q = new Vector3(
            rotation.X,
            rotation.Y,
            rotation.Z);

        var twiceCross = 2.0f * Vector3.Cross(q, vector);

        return vector +
               rotation.W * twiceCross +
               Vector3.Cross(q, twiceCross);
    }

    public static Quaternion Lerp(
        Quaternion start,
        Quaternion end,
        float amount)
    {
        start = start.Normalize();
        end = end.Normalize();
        if (Dot(start, end) < 0.0f)
        {
            end = -end;
        }

        return new Quaternion(
            start.X + (end.X - start.X) * amount,
            start.Y + (end.Y - start.Y) * amount,
            start.Z + (end.Z - start.Z) * amount,
            start.W + (end.W - start.W) * amount
        ).Normalize();
    }

    public static Quaternion Slerp(
    Quaternion start,
    Quaternion end,
    float amount)
    {
        start = start.Normalize();
        end = end.Normalize();

        var dot = Dot(start, end);

        // Ignore floating-point noise around zero.
        if (dot < -1e-6f)
        {
            end = -end;
            dot = -dot;
        }

        dot = System.Math.Clamp(dot, -1.0f, 1.0f);

        if (dot > 0.9995f)
        {
            return Lerp(start, end, amount);
        }

        var angle = MathF.Acos(dot);
        var sinAngle = MathF.Sin(angle);

        var startWeight =
            MathF.Sin((1.0f - amount) * angle) / sinAngle;

        var endWeight =
            MathF.Sin(amount * angle) / sinAngle;

        return (
            start * startWeight +
            end * endWeight
        ).Normalize();
    }

    public static Quaternion operator *(
        Quaternion left,
        Quaternion right)
    {
        return new Quaternion(
            left.W * right.X +
            left.X * right.W +
            left.Y * right.Z -
            left.Z * right.Y,

            left.W * right.Y -
            left.X * right.Z +
            left.Y * right.W +
            left.Z * right.X,

            left.W * right.Z +
            left.X * right.Y -
            left.Y * right.X +
            left.Z * right.W,

            left.W * right.W -
            left.X * right.X -
            left.Y * right.Y -
            left.Z * right.Z);
    }

    public static Quaternion operator +(
        Quaternion left,
        Quaternion right)
    {
        return new Quaternion(
            left.X + right.X,
            left.Y + right.Y,
            left.Z + right.Z,
            left.W + right.W);
    }

    public static Quaternion operator -(
        Quaternion value)
    {
        return new Quaternion(
            -value.X,
            -value.Y,
            -value.Z,
            -value.W);
    }

    public static Quaternion operator *(
        Quaternion value,
        float scalar)
    {
        return new Quaternion(
            value.X * scalar,
            value.Y * scalar,
            value.Z * scalar,
            value.W * scalar);
    }

    public static Quaternion operator /(
        Quaternion value,
        float scalar)
    {
        if (scalar == 0.0f)
        {
            throw new DivideByZeroException();
        }

        return new Quaternion(
            value.X / scalar,
            value.Y / scalar,
            value.Z / scalar,
            value.W / scalar);
    }
}
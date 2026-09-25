namespace Engine.Core.Math;

public readonly struct Fixed32 :
    IEquatable<Fixed32>,
    IComparable<Fixed32>
{
    private const int FractionBits = 16;
    private const long Scale = 1L << FractionBits;

    public int RawValue =>
    _raw;
    private readonly int _raw;

    public static bool NearlyEqual(
    Fixed32 left,
    Fixed32 right,
    Fixed32 tolerance)
    {
        return Abs(left - right) <= tolerance;
    }
    private Fixed32(int raw)
    {
        _raw = raw;
    }

    public static Fixed32 Zero =>
        new(0);

    public static Fixed32 One =>
        new((int)Scale);

    public static Fixed32 FromInt(
        int value)
    {
        return new(
            checked(value * (int)Scale));
    }

    public static Fixed32 FromFloat(
        float value)
    {
        return new(
            checked((int)MathF.Round(
                value * Scale)));
    }

    public int ToInt()
    {
        return _raw / (int)Scale;
    }

    public float ToFloat()
    {
        return _raw / (float)Scale;
    }

    public static Fixed32 operator +(
        Fixed32 left,
        Fixed32 right)
    {
        return new(
            checked(left._raw + right._raw));
    }

    public static Fixed32 operator -(
        Fixed32 left,
        Fixed32 right)
    {
        return new(
            checked(left._raw - right._raw));
    }

    public static Fixed32 operator *(
        Fixed32 left,
        Fixed32 right)
    {
        var value =
            (long)left._raw *
            right._raw;

        return new(
            checked((int)(value >> FractionBits)));
    }

    public static Fixed32 operator /(
        Fixed32 left,
        Fixed32 right)
    {
        if (right._raw == 0)
        {
            throw new DivideByZeroException();
        }

        var value =
            ((long)left._raw << FractionBits) /
            right._raw;

        return new(
            checked((int)value));
    }

    public static Fixed32 operator -(
        Fixed32 value)
    {
        return new(
            checked(-value._raw));
    }

    public static bool operator ==(
        Fixed32 left,
        Fixed32 right)
    {
        return left._raw == right._raw;
    }

    public static bool operator !=(
        Fixed32 left,
        Fixed32 right)
    {
        return left._raw != right._raw;
    }

    public static bool operator <(
        Fixed32 left,
        Fixed32 right)
    {
        return left._raw < right._raw;
    }

    public static bool operator >(
        Fixed32 left,
        Fixed32 right)
    {
        return left._raw > right._raw;
    }

    public static bool operator <=(
        Fixed32 left,
        Fixed32 right)
    {
        return left._raw <= right._raw;
    }

    public static bool operator >=(
        Fixed32 left,
        Fixed32 right)
    {
        return left._raw >= right._raw;
    }

    public bool Equals(
        Fixed32 other)
    {
        return _raw == other._raw;
    }

    public override bool Equals(
        object? obj)
    {
        return obj is Fixed32 other &&
               Equals(other);
    }

    public override int GetHashCode()
    {
        return _raw;
    }

    public int CompareTo(
        Fixed32 other)
    {
        return _raw.CompareTo(
            other._raw);
    }

    public override string ToString()
    {
        return ToFloat().ToString(
            System.Globalization.CultureInfo.InvariantCulture);
    }

    public static Fixed32 Abs(
    Fixed32 value)
    {
        return new(
            System.Math.Abs(value._raw));
    }

    public static Fixed32 Min(
        Fixed32 left,
        Fixed32 right)
    {
        return left._raw < right._raw
            ? left
            : right;
    }

    public static Fixed32 Max(
        Fixed32 left,
        Fixed32 right)
    {
        return left._raw > right._raw
            ? left
            : right;
    }

    public static Fixed32 Sqrt(
        Fixed32 value)
    {
        if (value._raw < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Cannot calculate square root of a negative value.");
        }

        if (value._raw == 0)
            return Zero;

        var scaled =
            ((long)value._raw) << FractionBits;

        var result =
            IntegerSqrt(scaled);

        return new(
            checked((int)result));
    }

    private static long IntegerSqrt(
        long value)
    {
        long result = 0;
        long bit = 1L << 62;

        while (bit > value)
        {
            bit >>= 2;
        }

        while (bit != 0)
        {
            if (value >= result + bit)
            {
                value -= result + bit;

                result =
                    (result >> 1) + bit;
            }
            else
            {
                result >>= 1;
            }

            bit >>= 2;
        }

        return result;
    }

    public static Fixed32 Clamp(
    Fixed32 value,
    Fixed32 min,
    Fixed32 max)
    {
        if (min > max)
        {
            throw new ArgumentException(
                "Minimum value cannot be greater than maximum value.");
        }

        if (value < min)
            return min;

        if (value > max)
            return max;

        return value;
    }

    public static Fixed32 FromRatio(
    int numerator,
    int denominator)
    {
        if (denominator == 0)
        {
            throw new DivideByZeroException();
        }

        var raw =
            (long)numerator *
            Scale /
            denominator;

        return new(
            checked((int)raw));
    }

    public int FloorToInt()
    {
        var scale =
            1 << 16;

        if (_raw >= 0)
            return _raw / scale;

        return -((-_raw + scale - 1) / scale);
    }
}
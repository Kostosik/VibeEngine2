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

    public static Fixed32 Pi =>
    new(205887);

    public static Fixed32 HalfPi =>
        new(102943);

    public static Fixed32 TwoPi =>
        new(411774);

    public static Fixed32 Sin(
    Fixed32 angle)
    {
        var raw =
            angle._raw %
            TwoPi._raw;

        if (raw > Pi._raw)
            raw -= TwoPi._raw;
        else if (raw < -Pi._raw)
            raw += TwoPi._raw;

        var sign = 1;

        if (raw > HalfPi._raw)
        {
            raw =
                Pi._raw -
                raw;
        }
        else if (raw < -HalfPi._raw)
        {
            raw =
                -Pi._raw -
                raw;
        }

        var x =
            new Fixed32(raw);

        var x2 =
            x * x;

        var x3 =
            x2 * x;

        var x5 =
            x3 * x2;

        var x7 =
            x5 * x2;

        return
            x
            - x3 * FromRatio(1, 6)
            + x5 * FromRatio(1, 120)
            - x7 * FromRatio(1, 5040);
    }

    public static Fixed32 Cos(
        Fixed32 angle)
    {
        var raw =
            angle._raw %
            TwoPi._raw;

        if (raw > Pi._raw)
            raw -= TwoPi._raw;
        else if (raw < -Pi._raw)
            raw += TwoPi._raw;

        var sign = 1;

        if (raw > HalfPi._raw)
        {
            raw =
                Pi._raw -
                raw;

            sign = -1;
        }
        else if (raw < -HalfPi._raw)
        {
            raw =
                -Pi._raw -
                raw;

            sign = -1;
        }

        var x =
            new Fixed32(raw);

        var x2 =
            x * x;

        var x4 =
            x2 * x2;

        var x6 =
            x4 * x2;

        var x8 =
            x4 * x4;

        var result =
            One
            - x2 * FromRatio(1, 2)
            + x4 * FromRatio(1, 24)
            - x6 * FromRatio(1, 720)
            + x8 * FromRatio(1, 40320);

        return sign > 0
            ? result
            : -result;
    }

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
        const long scale =
            1L << 16;

        var raw =
            (long)_raw;

        if (raw >= 0)
        {
            return (int)(raw / scale);
        }

        return (int)(
            -(
                (-raw + scale - 1) /
                scale));
    }
}
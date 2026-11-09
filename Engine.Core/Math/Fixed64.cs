using System.Globalization;

namespace Engine.Core.Math;

/// <summary>
/// Deterministic Q32.32 fixed-point number.
/// Arithmetic uses integer operations only.
/// Multiplication, division and ratios round to nearest,
/// with midpoint values rounded away from zero.
/// </summary>
public readonly struct Fixed64 :
    IEquatable<Fixed64>,
    IComparable<Fixed64>
{
    public const int FractionalBits = 32;
    public const long Scale = 1L << FractionalBits;

    private readonly long _raw;

    private Fixed64(long rawValue)
    {
        _raw = rawValue;
    }

    public long RawValue => _raw;

    public static Fixed64 Zero => new(0);
    public static Fixed64 One => new(Scale);
    public static Fixed64 Half => new(Scale / 2);
    public static Fixed64 Epsilon => new(1);

    public static Fixed64 MinValue => new(long.MinValue);
    public static Fixed64 MaxValue => new(long.MaxValue);

    public bool IsZero => _raw == 0;

    public static Fixed64 FromRaw(long rawValue)
    {
        return new Fixed64(rawValue);
    }

    public static Fixed64 FromInt(long value)
    {
        return new Fixed64(
            checked(value * Scale));
    }

    public static Fixed64 FromRatio(
        long numerator,
        long denominator)
    {
        if (denominator == 0)
            throw new DivideByZeroException();

        var scaledNumerator =
            (Int128)numerator * Scale;

        return new Fixed64(
            DivideRounded(
                scaledNumerator,
                denominator));
    }

    /// <summary>
    /// Converts a float at an external boundary.
    /// Do not use this method inside deterministic simulation logic.
    /// </summary>
    public static Fixed64 FromFloat(float value)
    {
        return FromDouble(value);
    }

    /// <summary>
    /// Converts a double at an external boundary.
    /// Do not use this method inside deterministic simulation logic.
    /// </summary>
    public static Fixed64 FromDouble(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Value must be finite.");
        }

        var scaled = value * Scale;

        if (!double.IsFinite(scaled))
            throw new OverflowException();

        var rounded = System.Math.Round(
            scaled,
            MidpointRounding.AwayFromZero);

        const double MinRawInclusive =
            -9223372036854775808d;

        const double MaxRawExclusive =
            9223372036854775808d;

        if (rounded < MinRawInclusive ||
            rounded >= MaxRawExclusive)
        {
            throw new OverflowException();
        }

        return new Fixed64(
            checked((long)rounded));
    }

    /// <summary>
    /// Converts to an integer, truncating toward zero.
    /// </summary>
    public int ToInt()
    {
        return checked((int)(_raw / Scale));
    }

    /// <summary>
    /// Converts to a double for presentation or external APIs.
    /// </summary>
    public double ToDouble()
    {
        return _raw / (double)Scale;
    }

    /// <summary>
    /// Converts to a float for presentation or external APIs.
    /// </summary>
    public float ToFloat()
    {
        return (float)ToDouble();
    }

    public int FloorToInt()
    {
        var result = _raw / Scale;

        if (_raw < 0 && _raw % Scale != 0)
            result--;

        return checked((int)result);
    }

    public int CeilingToInt()
    {
        var result = _raw / Scale;

        if (_raw > 0 && _raw % Scale != 0)
            result++;

        return checked((int)result);
    }

    public int RoundToInt()
    {
        return checked((int)DivideRounded(
            _raw,
            Scale));
    }

    public static Fixed64 Abs(Fixed64 value)
    {
        return new Fixed64(
            value._raw < 0
                ? checked(-value._raw)
                : value._raw);
    }

    /// <summary>
    /// Calculates the square root using integer arithmetic only.
    /// The result is rounded down to the nearest representable value.
    /// </summary>
    public static Fixed64 Sqrt(Fixed64 value)
    {
        if (value._raw < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Cannot calculate square root of a negative value.");
        }

        if (value._raw == 0)
            return Zero;

        // Q32.32 -> Q64.64 intermediate value.
        // UInt128 prevents overflow during scaling.
        var scaled =
            (UInt128)(ulong)value._raw << FractionalBits;

        var result = IntegerSquareRoot(scaled);

        return new Fixed64(
            checked((long)result));
    }

    public static Fixed64 Min(
        Fixed64 left,
        Fixed64 right)
    {
        return left._raw <= right._raw
            ? left
            : right;
    }

    public static Fixed64 Max(
        Fixed64 left,
        Fixed64 right)
    {
        return left._raw >= right._raw
            ? left
            : right;
    }

    public static Fixed64 Clamp(
        Fixed64 value,
        Fixed64 min,
        Fixed64 max)
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

    public static int Sign(Fixed64 value)
    {
        return value._raw.CompareTo(0L);
    }

    public static Fixed64 operator +(
        Fixed64 left,
        Fixed64 right)
    {
        return new Fixed64(
            checked(left._raw + right._raw));
    }

    public static Fixed64 operator -(
        Fixed64 left,
        Fixed64 right)
    {
        return new Fixed64(
            checked(left._raw - right._raw));
    }

    public static Fixed64 operator *(
        Fixed64 left,
        Fixed64 right)
    {
        var product =
            (Int128)left._raw * right._raw;

        return new Fixed64(
            DivideRounded(product, Scale));
    }

    public static Fixed64 operator /(
        Fixed64 left,
        Fixed64 right)
    {
        if (right._raw == 0)
            throw new DivideByZeroException();

        var numerator =
            (Int128)left._raw << FractionalBits;

        return new Fixed64(
            DivideRounded(numerator, right._raw));
    }

    public static Fixed64 operator -(
        Fixed64 value)
    {
        return new Fixed64(
            checked(-value._raw));
    }

    public static bool operator ==(
        Fixed64 left,
        Fixed64 right)
    {
        return left._raw == right._raw;
    }

    public static bool operator !=(
        Fixed64 left,
        Fixed64 right)
    {
        return left._raw != right._raw;
    }

    public static bool operator <(
        Fixed64 left,
        Fixed64 right)
    {
        return left._raw < right._raw;
    }

    public static bool operator >(
        Fixed64 left,
        Fixed64 right)
    {
        return left._raw > right._raw;
    }

    public static bool operator <=(
        Fixed64 left,
        Fixed64 right)
    {
        return left._raw <= right._raw;
    }

    public static bool operator >=(
        Fixed64 left,
        Fixed64 right)
    {
        return left._raw >= right._raw;
    }

    public bool Equals(Fixed64 other)
    {
        return _raw == other._raw;
    }

    public override bool Equals(object? obj)
    {
        return obj is Fixed64 other &&
               Equals(other);
    }

    public override int GetHashCode()
    {
        return _raw.GetHashCode();
    }

    public int CompareTo(Fixed64 other)
    {
        return _raw.CompareTo(other._raw);
    }

    public override string ToString()
    {
        return ToDouble().ToString(
            CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Calculates floor(sqrt(value)) using the restoring integer
    /// square-root algorithm.
    /// </summary>
    private static UInt128 IntegerSquareRoot(UInt128 value)
    {
        UInt128 result = 0;

        // Highest power of four representable by UInt128.
        UInt128 bit = (UInt128)1 << 126;

        while (bit > value)
            bit >>= 2;

        while (bit != 0)
        {
            if (value >= result + bit)
            {
                value -= result + bit;
                result = (result >> 1) + bit;
            }
            else
            {
                result >>= 1;
            }

            bit >>= 2;
        }

        return result;
    }

    /// <summary>
    /// Divides two integers and rounds to nearest.
    /// Exact midpoint values are rounded away from zero.
    /// </summary>
    private static long DivideRounded(
        Int128 numerator,
        Int128 denominator)
    {
        if (denominator == 0)
            throw new DivideByZeroException();

        var negative =
            (numerator < 0) != (denominator < 0);

        var absoluteNumerator =
            numerator < 0
                ? -numerator
                : numerator;

        var absoluteDenominator =
            denominator < 0
                ? -denominator
                : denominator;

        var quotient =
            absoluteNumerator / absoluteDenominator;

        var remainder =
            absoluteNumerator % absoluteDenominator;

        if (remainder * 2 >= absoluteDenominator)
            quotient++;

        var result = negative
            ? -quotient
            : quotient;

        if (result < (Int128)long.MinValue ||
            result > (Int128)long.MaxValue)
        {
            throw new OverflowException();
        }

        return (long)result;
    }
}
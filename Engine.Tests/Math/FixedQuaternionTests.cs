
using Engine.Core.Math;

namespace Engine.Tests.Math;

public sealed class FixedQuaternionTests
{
    private static readonly Fixed32 Tolerance =
        Fixed32.FromFloat(0.002f);

    [Fact]
    public void Identity_IsCorrect()
    {
        var value = FixedQuaternion.Identity;

        Assert.Equal(Fixed32.Zero, value.X);
        Assert.Equal(Fixed32.Zero, value.Y);
        Assert.Equal(Fixed32.Zero, value.Z);
        Assert.Equal(Fixed32.One, value.W);
    }

    [Fact]
    public void Normalize_ReturnsUnitQuaternion()
    {
        var value = new FixedQuaternion(
            Fixed32.FromInt(1),
            Fixed32.FromInt(2),
            Fixed32.FromInt(3),
            Fixed32.FromInt(4));

        var normalized = value.Normalize();

        Assert.True(
            Fixed32.NearlyEqual(
                normalized.Length(),
                Fixed32.One,
                Tolerance));
    }

    [Fact]
    public void Normalize_ZeroReturnsIdentity()
    {
        var value = new FixedQuaternion(
            Fixed32.Zero,
            Fixed32.Zero,
            Fixed32.Zero,
            Fixed32.Zero);

        Assert.Equal(
            FixedQuaternion.Identity,
            value.Normalize());
    }

    [Fact]
    public void Conjugate_NegatesVectorPart()
    {
        var value = new FixedQuaternion(
            Fixed32.FromInt(1),
            Fixed32.FromInt(2),
            Fixed32.FromInt(3),
            Fixed32.FromInt(4));

        var result = value.Conjugate();

        Assert.Equal(Fixed32.FromInt(-1), result.X);
        Assert.Equal(Fixed32.FromInt(-2), result.Y);
        Assert.Equal(Fixed32.FromInt(-3), result.Z);
        Assert.Equal(Fixed32.FromInt(4), result.W);
    }

    [Fact]
    public void Multiplication_ComposesRotations()
    {
        var first = FixedQuaternion.CreateFromAxisAngle(
            FixedVector3.UnitZ,
            Fixed32.HalfPi);

        var second = FixedQuaternion.CreateFromAxisAngle(
            FixedVector3.UnitX,
            Fixed32.HalfPi);

        var result = (first * second).Normalize();

        Assert.True(
            Fixed32.NearlyEqual(
                result.Length(),
                Fixed32.One,
                Tolerance));
    }

    [Fact]
    public void CreateFromAxisAngle_RotatesAroundZ()
    {
        var rotation = FixedQuaternion.CreateFromAxisAngle(
            FixedVector3.UnitZ,
            Fixed32.HalfPi);

        var result = rotation.Transform(FixedVector3.UnitX);

        Assert.True(
            Fixed32.NearlyEqual(
                result.X,
                Fixed32.Zero,
                Tolerance));

        Assert.True(
            Fixed32.NearlyEqual(
                result.Y,
                Fixed32.One,
                Tolerance));

        Assert.True(
            Fixed32.NearlyEqual(
                result.Z,
                Fixed32.Zero,
                Tolerance));
    }

    [Fact]
    public void CreateFromAxisAngle_ZeroAxisReturnsIdentity()
    {
        var result = FixedQuaternion.CreateFromAxisAngle(
            FixedVector3.Zero,
            Fixed32.HalfPi);

        Assert.Equal(FixedQuaternion.Identity, result);
    }

    [Fact]
    public void Transform_IdentityPreservesVector()
    {
        var vector = new FixedVector3(
            Fixed32.FromInt(2),
            Fixed32.FromInt(3),
            Fixed32.FromInt(4));

        var result = FixedQuaternion.Identity.Transform(vector);

        Assert.Equal(vector, result);
    }

    [Fact]
    public void Inverse_ComposedWithOriginalReturnsIdentity()
    {
        var rotation = FixedQuaternion.CreateFromAxisAngle(
            FixedVector3.UnitY,
            Fixed32.HalfPi);

        var result = (rotation * rotation.Inverse()).Normalize();

        Assert.True(
            Fixed32.NearlyEqual(
                result.X,
                Fixed32.Zero,
                Tolerance));

        Assert.True(
            Fixed32.NearlyEqual(
                result.Y,
                Fixed32.Zero,
                Tolerance));

        Assert.True(
            Fixed32.NearlyEqual(
                result.Z,
                Fixed32.Zero,
                Tolerance));

        Assert.True(
            Fixed32.NearlyEqual(
                result.W,
                Fixed32.One,
                Tolerance));
    }

    [Fact]
    public void Dot_IsCorrect()
    {
        var first = new FixedQuaternion(
            Fixed32.FromInt(1),
            Fixed32.FromInt(2),
            Fixed32.FromInt(3),
            Fixed32.FromInt(4));

        var second = new FixedQuaternion(
            Fixed32.FromInt(5),
            Fixed32.FromInt(6),
            Fixed32.FromInt(7),
            Fixed32.FromInt(8));

        Assert.Equal(
            Fixed32.FromInt(70),
            first.Dot(second));
    }
}

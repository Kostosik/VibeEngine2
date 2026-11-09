using Engine.Core.Math;

namespace Engine.Tests.Math;

public sealed class QuaternionTests
{
    [Fact]
    public void Identity_DoesNotRotateVector()
    {
        var vector = new Vector3(1, 2, 3);

        var result = Quaternion.Identity.Transform(vector);

        Assert.Equal(vector, result);
    }

    [Fact]
    public void CreateFromAxisAngle_RotatesAroundZ()
    {
        var rotation = Quaternion.CreateFromAxisAngle(
            Vector3.UnitZ,
            MathF.PI / 2.0f);

        var result = rotation.Transform(Vector3.UnitX);

        Assert.Equal(0.0f, result.X, 5);
        Assert.Equal(1.0f, result.Y, 5);
        Assert.Equal(0.0f, result.Z, 5);
    }

    [Fact]
    public void Normalize_ReturnsUnitQuaternion()
    {
        var quaternion = new Quaternion(2, 0, 0, 0);

        var result = quaternion.Normalize();

        Assert.Equal(1.0f, result.Length(), 5);
    }

    [Fact]
    public void Normalize_ZeroReturnsIdentity()
    {
        var result = new Quaternion(0, 0, 0, 0).Normalize();

        Assert.Equal(Quaternion.Identity, result);
    }

    [Fact]
    public void Conjugate_IsCorrect()
    {
        var quaternion = new Quaternion(1, 2, 3, 4);

        var result = quaternion.Conjugate();

        Assert.Equal(
            new Quaternion(-1, -2, -3, 4),
            result);
    }

    [Fact]
    public void Inverse_MultipliedByOriginalReturnsIdentity()
    {
        var quaternion = new Quaternion(1, 2, 3, 4);

        var result = quaternion * quaternion.Inverse();

        Assert.Equal(0.0f, result.X, 5);
        Assert.Equal(0.0f, result.Y, 5);
        Assert.Equal(0.0f, result.Z, 5);
        Assert.Equal(1.0f, result.W, 5);
    }

    [Fact]
    public void Inverse_ZeroThrows()
    {
        var quaternion = new Quaternion(0, 0, 0, 0);

        Assert.Throws<InvalidOperationException>(
            () => quaternion.Inverse());
    }

    [Fact]
    public void Dot_IsCorrect()
    {
        var result = Quaternion.Dot(
            new Quaternion(1, 2, 3, 4),
            new Quaternion(5, 6, 7, 8));

        Assert.Equal(70.0f, result);
    }

    [Fact]
    public void Multiplication_ComposesRotations()
    {
        var first = Quaternion.CreateFromAxisAngle(
            Vector3.UnitZ,
            MathF.PI / 2.0f);

        var second = Quaternion.CreateFromAxisAngle(
            Vector3.UnitZ,
            MathF.PI / 2.0f);

        var combined = first * second;
        var result = combined.Transform(Vector3.UnitX);

        Assert.Equal(-1.0f, result.X, 5);
        Assert.Equal(0.0f, result.Y, 5);
        Assert.Equal(0.0f, result.Z, 5);
    }

    [Fact]
    public void Lerp_ReturnsMidpointRotation()
    {
        var end = Quaternion.CreateFromAxisAngle(
            Vector3.UnitZ,
            MathF.PI / 2.0f);

        var result = Quaternion.Lerp(
            Quaternion.Identity,
            end,
            0.5f);

        var rotated = result.Transform(Vector3.UnitX);

        Assert.Equal(1.0f, result.Length(), 5);
        Assert.Equal(0.7071f, rotated.X, 3);
        Assert.Equal(0.7071f, rotated.Y, 3);
    }

    [Fact]
    public void Slerp_ReturnsMidpointRotation()
    {
        var end = Quaternion.CreateFromAxisAngle(
            Vector3.UnitZ,
            MathF.PI);

        var result = Quaternion.Slerp(
            Quaternion.Identity,
            end,
            0.5f);

        var rotated = result.Transform(Vector3.UnitX);

        Assert.Equal(0.0f, rotated.X, 5);
        Assert.Equal(1.0f, rotated.Y, 5);
        Assert.Equal(0.0f, rotated.Z, 5);
    }

    [Fact]
    public void CreateFromAxisAngle_ZeroAxisReturnsIdentity()
    {
        var result = Quaternion.CreateFromAxisAngle(
            Vector3.Zero,
            MathF.PI);

        Assert.Equal(Quaternion.Identity, result);
    }
}
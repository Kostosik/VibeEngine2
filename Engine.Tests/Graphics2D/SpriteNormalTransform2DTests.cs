using Engine.Core.Math;
using Engine.Graphics.Commands;
using Xunit;

namespace Engine.Tests.Graphics2D;

public sealed class SpriteNormalTransform2DTests
{
    [Fact]
    public void Identity_PreservesNormal()
    {
        var transform =
            SpriteNormalTransform2D.Identity;

        var normal = new Vector3(
            0.5f,
            0.5f,
            0.7071067f).Normalize();

        var result =
            transform.TransformNormal(normal);

        AssertVectorApproximately(normal, result);
    }

    [Fact]
    public void Create_RotatesNormalIntoLightingCoordinates()
    {
        var transform =
            SpriteNormalTransform2D.Create(
                Vector2.One,
                90.0f);

        var result =
            transform.TransformNormal(Vector3.UnitX);

        AssertVectorApproximately(
            new Vector3(0, -1, 0),
            result);
    }

    [Fact]
    public void Create_FlipX_ChangesNormalDirection()
    {
        var transform =
            SpriteNormalTransform2D.Create(
                Vector2.One,
                0.0f,
                flipX: true);

        var result =
            transform.TransformNormal(Vector3.UnitX);

        AssertVectorApproximately(
            -Vector3.UnitX,
            result);
    }

    [Fact]
    public void Create_FlipY_ChangesNormalDirection()
    {
        var transform =
            SpriteNormalTransform2D.Create(
                Vector2.One,
                0.0f,
                flipY: true);

        var result =
            transform.TransformNormal(Vector3.UnitY);

        AssertVectorApproximately(
            -Vector3.UnitY,
            result);
    }

    [Fact]
    public void Create_PreservesZComponentWhenFlipping()
    {
        var transform =
            SpriteNormalTransform2D.Create(
                Vector2.One,
                0.0f,
                flipX: true,
                flipY: true);

        var result =
            transform.TransformNormal(
                new Vector3(0, 0, 1));

        Assert.Equal(Vector3.UnitZ, result);
    }

    [Fact]
    public void Create_NonUniformScale_UsesInverseScale()
    {
        var transform =
            SpriteNormalTransform2D.Create(
                new Vector2(2.0f, 1.0f),
                0.0f);

        var input =
            new Vector3(1, 0, 1).Normalize();

        var result =
            transform.TransformNormal(input);

        var expected =
            new Vector3(0.5f, 0, 1).Normalize();

        AssertVectorApproximately(
            expected,
            result);
    }

    [Fact]
    public void Create_RejectsZeroScale()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SpriteNormalTransform2D.Create(
                new Vector2(0, 1),
                0));
    }

    [Fact]
    public void Create_RejectsInvalidRotation()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SpriteNormalTransform2D.Create(
                Vector2.One,
                float.NaN));
    }

    private static void AssertVectorApproximately(
        Vector3 expected,
        Vector3 actual)
    {
        Assert.InRange(
            MathF.Abs(expected.X - actual.X),
            0.0f,
            0.0001f);

        Assert.InRange(
            MathF.Abs(expected.Y - actual.Y),
            0.0f,
            0.0001f);

        Assert.InRange(
            MathF.Abs(expected.Z - actual.Z),
            0.0f,
            0.0001f);
    }
}
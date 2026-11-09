using Engine.Core.Math;

namespace Engine.Tests.Math;

public sealed class Matrix4x4Tests
{
    [Fact]
    public void Identity_DoesNotChangePoint()
    {
        var point = new Vector3(1, 2, 3);

        var result = Matrix4x4.Identity.TransformPoint(point);

        Assert.Equal(point, result);
    }

    [Fact]
    public void Translation_MovesPoint()
    {
        var matrix = Matrix4x4.CreateTranslation(
            new Vector3(10, 20, 30));

        var result = matrix.TransformPoint(
            new Vector3(1, 2, 3));

        Assert.Equal(new Vector3(11, 22, 33), result);
    }

    [Fact]
    public void Scale_ScalesPoint()
    {
        var matrix = Matrix4x4.CreateScale(
            new Vector3(2, 3, 4));

        var result = matrix.TransformPoint(
            new Vector3(1, 2, 3));

        Assert.Equal(new Vector3(2, 6, 12), result);
    }

    [Fact]
    public void Rotation_RotatesPoint()
    {
        var rotation = Quaternion.CreateFromAxisAngle(
            Vector3.UnitZ,
            MathF.PI / 2.0f);

        var matrix = Matrix4x4.CreateRotation(rotation);

        var result = matrix.TransformPoint(Vector3.UnitX);

        Assert.Equal(0.0f, result.X, 5);
        Assert.Equal(1.0f, result.Y, 5);
        Assert.Equal(0.0f, result.Z, 5);
    }

    [Fact]
    public void TransformDirection_DoesNotApplyTranslation()
    {
        var matrix = Matrix4x4.CreateTranslation(
            new Vector3(10, 20, 30));

        var result = matrix.TransformDirection(Vector3.UnitX);

        Assert.Equal(Vector3.UnitX, result);
    }

    [Fact]
    public void MatrixMultiplication_ComposesTransforms()
    {
        var scale = Matrix4x4.CreateScale(2);

        var translation = Matrix4x4.CreateTranslation(
            new Vector3(10, 0, 0));

        var matrix = scale * translation;

        var result = matrix.TransformPoint(Vector3.UnitX);

        Assert.Equal(new Vector3(12, 0, 0), result);
    }

    [Fact]
    public void PerspectiveProjection_MapsNearPlaneToMinusOne()
    {
        var matrix = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 2.0f,
            1.0f,
            1.0f,
            100.0f);

        var result = matrix.TransformPoint(
            new Vector3(0, 0, -1));

        Assert.Equal(-1.0f, result.Z, 5);
    }

    [Fact]
    public void PerspectiveProjection_MapsFarPlaneToOne()
    {
        var matrix = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 2.0f,
            1.0f,
            1.0f,
            100.0f);

        var result = matrix.TransformPoint(
            new Vector3(0, 0, -100));

        Assert.Equal(1.0f, result.Z, 5);
    }

    [Fact]
    public void OrthographicProjection_MapsNearPlaneToMinusOne()
    {
        var matrix = Matrix4x4.CreateOrthographic(
            10, 10, 1, 100);

        var result = matrix.TransformPoint(
            new Vector3(0, 0, -1));

        Assert.Equal(-1.0f, result.Z, 5);
    }

    [Fact]
    public void OrthographicProjection_MapsFarPlaneToOne()
    {
        var matrix = Matrix4x4.CreateOrthographic(
            10, 10, 1, 100);

        var result = matrix.TransformPoint(
            new Vector3(0, 0, -100));

        Assert.Equal(1.0f, result.Z, 5);
    }

    [Fact]
    public void PerspectiveProjection_InvalidParametersThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Matrix4x4.CreatePerspectiveFieldOfView(
                MathF.PI,
                1,
                1,
                100));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Matrix4x4.CreatePerspectiveFieldOfView(
                1,
                0,
                1,
                100));
    }

    [Fact]
    public void OrthographicProjection_InvalidParametersThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Matrix4x4.CreateOrthographic(
                0, 10, 1, 100));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Matrix4x4.CreateOrthographic(
                10, 10, 100, 1));
    }
}
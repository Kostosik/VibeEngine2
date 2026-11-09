using Engine.Core.Math;
using Engine.Transform;

namespace Engine.Tests.Transform;

public sealed class Transform3DTests
{
    [Fact]
    public void IdentityHasExpectedValues()
    {
        var transform = Transform3D.Identity;

        Assert.Equal(Vector3.Zero, transform.Position);
        Assert.Equal(Quaternion.Identity, transform.Rotation);
        Assert.Equal(Vector3.One, transform.Scale);
        Assert.True(transform.IsFinite);
    }

    [Fact]
    public void CombineAppliesTranslation()
    {
        var parent = new Transform3D(
            new Vector3(10, 20, 30));

        var local = new Transform3D(
            new Vector3(1, 2, 3));

        var world = TransformOperations3D.Combine(
            parent,
            local);

        Assert.Equal(
            new Vector3(11, 22, 33),
            world.Position);
    }

    [Fact]
    public void CombineAppliesRotationToLocalPosition()
    {
        var parent = new Transform3D(Vector3.Zero)
        {
            Rotation = Quaternion.CreateFromAxisAngle(
                Vector3.UnitZ,
                MathF.PI / 2.0f)
        };

        var local = new Transform3D(Vector3.UnitX);

        var world = TransformOperations3D.Combine(
            parent,
            local);

        Assert.Equal(0.0f, world.Position.X, 5);
        Assert.Equal(1.0f, world.Position.Y, 5);
        Assert.Equal(0.0f, world.Position.Z, 5);
    }

    [Fact]
    public void CombineAppliesScale()
    {
        var parent = new Transform3D(Vector3.Zero)
        {
            Scale = new Vector3(2, 3, 4)
        };

        var local = new Transform3D(
            new Vector3(1, 2, 3))
        {
            Scale = new Vector3(5, 6, 7)
        };

        var world = TransformOperations3D.Combine(
            parent,
            local);

        Assert.Equal(
            new Vector3(2, 6, 12),
            world.Position);

        Assert.Equal(
            new Vector3(10, 18, 28),
            world.Scale);
    }

    [Fact]
    public void CombineAccumulatesRotation()
    {
        var parent = new Transform3D(Vector3.Zero)
        {
            Rotation = Quaternion.CreateFromAxisAngle(
                Vector3.UnitZ,
                MathF.PI / 2.0f)
        };

        var local = new Transform3D(Vector3.Zero)
        {
            Rotation = Quaternion.CreateFromAxisAngle(
                Vector3.UnitZ,
                MathF.PI / 2.0f)
        };

        var world = TransformOperations3D.Combine(
            parent,
            local);

        var result = world.Rotation.Transform(Vector3.UnitX);

        Assert.Equal(-1.0f, result.X, 5);
        Assert.Equal(0.0f, result.Y, 5);
        Assert.Equal(0.0f, result.Z, 5);
    }

    [Fact]
    public void TransformPointAppliesFullTransform()
    {
        var transform = new Transform3D(
            new Vector3(10, 20, 30))
        {
            Rotation = Quaternion.CreateFromAxisAngle(
                Vector3.UnitZ,
                MathF.PI / 2.0f),

            Scale = new Vector3(2, 2, 2)
        };

        var result = TransformOperations3D.TransformPoint(
            transform,
            Vector3.UnitX);

        Assert.Equal(10.0f, result.X, 5);
        Assert.Equal(22.0f, result.Y, 5);
        Assert.Equal(30.0f, result.Z, 5);
    }

    [Fact]
    public void TransformDirectionIgnoresPositionAndScale()
    {
        var transform = new Transform3D(
            new Vector3(100, 200, 300))
        {
            Rotation = Quaternion.CreateFromAxisAngle(
                Vector3.UnitZ,
                MathF.PI / 2.0f),

            Scale = new Vector3(10, 10, 10)
        };

        var result = TransformOperations3D.TransformDirection(
            transform,
            Vector3.UnitX);

        Assert.Equal(0.0f, result.X, 5);
        Assert.Equal(1.0f, result.Y, 5);
        Assert.Equal(0.0f, result.Z, 5);
    }

    [Fact]
    public void TranslateChangesPosition()
    {
        var transform = Transform3D.Identity;

        var result = TransformOperations3D.Translate(
            transform,
            new Vector3(1, 2, 3));

        Assert.Equal(
            new Vector3(1, 2, 3),
            result.Position);
    }

    [Fact]
    public void RotateChangesRotation()
    {
        var transform = Transform3D.Identity;

        var result = TransformOperations3D.Rotate(
            transform,
            Quaternion.CreateFromAxisAngle(
                Vector3.UnitY,
                MathF.PI / 2.0f));

        var direction = result.Rotation.Transform(Vector3.UnitZ);

        Assert.Equal(1.0f, direction.X, 5);
        Assert.Equal(0.0f, direction.Y, 5);
        Assert.Equal(0.0f, direction.Z, 5);
    }

    [Fact]
    public void ScaleByChangesScale()
    {
        var transform = Transform3D.Identity;

        var result = TransformOperations3D.ScaleBy(
            transform,
            new Vector3(2, 3, 4));

        Assert.Equal(
            new Vector3(2, 3, 4),
            result.Scale);
    }

    [Fact]
    public void NonFiniteValuesAreDetected()
    {
        var transform = Transform3D.Identity;
        transform.Position = new Vector3(float.NaN, 0, 0);

        Assert.False(transform.IsFinite);
    }
}
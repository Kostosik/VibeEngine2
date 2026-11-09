using Engine.Core.Math;
using Engine.Transform;
using Xunit;

namespace Engine.Tests.Transform;

public class FixedTransform3DTests
{
    [Fact]
    public void Identity_HasDefaultValues()
    {
        var transform = FixedTransform3D.Identity;

        Assert.Equal(FixedVector3.Zero, transform.Position);
        Assert.Equal(FixedQuaternion.Identity, transform.Rotation);
        Assert.Equal(FixedVector3.One, transform.Scale);
    }

    [Fact]
    public void TransformPoint_AppliesScaleAndPosition()
    {
        var transform = new FixedTransform3D(
            new FixedVector3(
                Fixed32.FromInt(10),
                Fixed32.FromInt(20),
                Fixed32.FromInt(30)))
        {
            Scale = new FixedVector3(
                Fixed32.FromInt(2),
                Fixed32.FromInt(3),
                Fixed32.FromInt(4))
        };

        var point = new FixedVector3(
            Fixed32.One,
            Fixed32.One,
            Fixed32.One);

        var result = FixedTransformOperations3D.TransformPoint(
            transform,
            point);

        Assert.Equal(
            new FixedVector3(
                Fixed32.FromInt(12),
                Fixed32.FromInt(23),
                Fixed32.FromInt(34)),
            result);
    }

    [Fact]
    public void Combine_AppliesParentTransform()
    {
        var parent = new FixedTransform3D(
            new FixedVector3(
                Fixed32.FromInt(10),
                Fixed32.Zero,
                Fixed32.Zero))
        {
            Scale = new FixedVector3(
                Fixed32.FromInt(2),
                Fixed32.FromInt(2),
                Fixed32.FromInt(2))
        };

        var local = new FixedTransform3D(
            new FixedVector3(
                Fixed32.FromInt(3),
                Fixed32.Zero,
                Fixed32.Zero));

        var result = FixedTransformOperations3D.Combine(
            parent,
            local);

        Assert.Equal(
            Fixed32.FromInt(16),
            result.Position.X);
    }

    [Fact]
    public void Rotate_AppliesRotation()
    {
        var transform = FixedTransform3D.Identity;

        var rotation = FixedQuaternion.CreateFromAxisAngle(
            FixedVector3.UnitZ,
            Fixed32.HalfPi);

        transform = FixedTransformOperations3D.Rotate(
            transform,
            rotation);

        var result = FixedTransformOperations3D.TransformPoint(
            transform,
            FixedVector3.UnitX);

        var tolerance = Fixed32.FromFloat(0.01f);

        Assert.True(Fixed32.Abs(result.X) <= tolerance);
        Assert.True(
            Fixed32.Abs(result.Y - Fixed32.One) <= tolerance);
    }

    [Fact]
    public void TransformDirection_DoesNotApplyPositionOrScale()
    {
        var transform = new FixedTransform3D(
            new FixedVector3(
                Fixed32.FromInt(100),
                Fixed32.FromInt(200),
                Fixed32.FromInt(300)))
        {
            Scale = new FixedVector3(
                Fixed32.FromInt(5),
                Fixed32.FromInt(5),
                Fixed32.FromInt(5))
        };

        var result = FixedTransformOperations3D.TransformDirection(
            transform,
            FixedVector3.UnitX);

        Assert.Equal(FixedVector3.UnitX, result);
    }
}
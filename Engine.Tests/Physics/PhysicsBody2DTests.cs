using Engine.Core.Math;
using Engine.Physics.Components;

namespace Engine.Tests.Physics;

public sealed class PhysicsBody2DTests
{
    [Fact]
    public void Constructor_RejectsUnknownBodyType()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PhysicsBody2D(
                (PhysicsBodyType)999,
                Fixed32.One));
    }

    [Fact]
    public void BodyType_RejectsUnknownValue()
    {
        var body =
            PhysicsBody2D.Dynamic(Fixed32.One);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => body.BodyType = (PhysicsBodyType)999);

        Assert.Equal(
            PhysicsBodyType.Dynamic,
            body.BodyType);
    }
}
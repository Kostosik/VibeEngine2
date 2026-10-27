using Engine.Physics.Shapes;

namespace Engine.Tests.Physics;

public sealed class PhysicsShape2DTests
{
    [Fact]
    public void FromAabb_RejectsDefaultShape()
    {
        Assert.Throws<ArgumentException>(
            () => PhysicsShape2D.FromAabb(
                default(AabbShape2D)));
    }

    [Fact]
    public void FromCircle_RejectsDefaultShape()
    {
        Assert.Throws<ArgumentException>(
            () => PhysicsShape2D.FromCircle(
                default(CircleShape2D)));
    }

    [Fact]
    public void FromPolygon_RejectsDefaultShape()
    {
        Assert.Throws<ArgumentException>(
            () => PhysicsShape2D.FromPolygon(
                default(PolygonShape2D)));
    }
}
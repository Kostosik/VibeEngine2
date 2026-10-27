using Engine.Core.Math;
using Engine.Physics.Components;
using Engine.Physics.Shapes;

namespace Engine.Tests.Physics;

public sealed class Collider2DTests
{
    [Fact]
    public void Constructor_RejectsDefaultShape()
    {
        Assert.Throws<ArgumentException>(
            () => new Collider2D(default(AabbShape2D)));
    }

    [Fact]
    public void ShapeSetter_RejectsUninitializedShape()
    {
        var collider = new Collider2D(
            new CircleShape2D(Fixed32.One));

        Assert.Throws<ArgumentException>(
            () => collider.Shape = default);

        Assert.Equal(
            PhysicsShapeType.Circle,
            collider.Shape.Type);
    }
}
using Engine.Core.Math;
using Engine.Physics;

namespace Engine.Tests.Physics;

public sealed class PhysicsSettings2DTests
{
    [Fact]
    public void VelocityIterations_CannotBeNegative()
    {
        var settings = new PhysicsSettings2D();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => settings.VelocityIterations = -1);
    }

    [Fact]
    public void Substeps_CannotBeZeroOrNegative()
    {
        var settings =
            new PhysicsSettings2D();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => settings.Substeps = 0);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => settings.Substeps = -1);
    }

    [Fact]
    public void PositionIterations_CannotBeNegative()
    {
        var settings = new PhysicsSettings2D();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => settings.PositionIterations = -1);
    }

    [Fact]
    public void PenetrationSlop_CannotBeNegative()
    {
        var settings = new PhysicsSettings2D();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => settings.PenetrationSlop =
                Fixed32.FromInt(-1));
    }

    [Fact]
    public void PositionCorrectionPercent_MustBeBetweenZeroAndOne()
    {
        var settings = new PhysicsSettings2D();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => settings.PositionCorrectionPercent =
                Fixed32.FromInt(2));
    }
}
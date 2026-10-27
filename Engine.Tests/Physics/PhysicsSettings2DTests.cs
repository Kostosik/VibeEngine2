using Engine.Core.Determinism;
using Engine.Core.Math;
using Engine.Physics;

namespace Engine.Tests.Physics;

public sealed class PhysicsSettings2DTests
{
    [Fact]
    public void Settings_AffectDeterministicHash()
    {
        var defaultHash = GetHash(
            new PhysicsSettings2D());

        var variants = new[]
        {
        new PhysicsSettings2D
        {
            Gravity = new FixedVector2(
                Fixed32.One,
                Fixed32.FromInt(2))
        },
        new PhysicsSettings2D
        {
            VelocityIterations = 5
        },
        new PhysicsSettings2D
        {
            PositionIterations = 3
        },
        new PhysicsSettings2D
        {
            Substeps = 5
        },
        new PhysicsSettings2D
        {
            PenetrationSlop = Fixed32.FromRatio(1, 100)
        },
        new PhysicsSettings2D
        {
            PositionCorrectionPercent =
                Fixed32.FromRatio(1, 2)
        },
        new PhysicsSettings2D
        {
            SleepLinearVelocityThreshold = Fixed32.One
        },
        new PhysicsSettings2D
        {
            SleepAngularVelocityThreshold = Fixed32.One
        },
        new PhysicsSettings2D
        {
            SleepTime = Fixed32.One
        }
    };

        foreach (var settings in variants)
        {
            Assert.NotEqual(
                defaultHash,
                GetHash(settings));
        }
    }

    private static DeterministicStateHash GetHash(
        PhysicsSettings2D settings)
    {
        var hasher = DeterministicStateHasher.Create();

        settings.AddToHash(ref hasher);

        return hasher.GetHash();
    }

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
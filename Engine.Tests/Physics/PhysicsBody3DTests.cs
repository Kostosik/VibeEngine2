using Engine.Core.Math;
using Engine.Physics.ThreeD.Components;
using Xunit;

namespace Engine.Tests.Physics;

public sealed class PhysicsBody3DTests
{
    [Fact]
    public void DynamicBody_UsesConfiguredMassAndInertia()
    {
        var body = PhysicsBody3D.Dynamic(
            Fixed32.FromInt(2),
            new FixedVector3(
                Fixed32.FromInt(2),
                Fixed32.FromInt(4),
                Fixed32.FromInt(8)));

        Assert.Equal(
            Fixed32.FromRatio(1, 2),
            body.InverseMass);

        Assert.Equal(
            new FixedVector3(
                Fixed32.FromRatio(1, 2),
                Fixed32.FromRatio(1, 4),
                Fixed32.FromRatio(1, 8)),
            body.InverseInertia);
    }

    [Fact]
    public void StaticBody_HasZeroInverseMass()
    {
        var body = PhysicsBody3D.Static();

        Assert.Equal(
            Fixed32.Zero,
            body.InverseMass);

        Assert.Equal(
            FixedVector3.Zero,
            body.InverseInertia);
    }

    [Fact]
    public void AddForce_WakesSleepingBody()
    {
        var body = PhysicsBody3D.Dynamic(Fixed32.One);

        body.Sleep();

        Assert.True(body.IsSleeping);

        body.AddForce(FixedVector3.UnitY);

        Assert.False(body.IsSleeping);

        body.ClearForces();

        Assert.Equal(
            FixedVector3.Zero,
            body.Force);
    }
}
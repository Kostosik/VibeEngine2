using Engine.Core.Determinism;
using Engine.Core.Math;
using Engine.Transform;
using Xunit;

namespace Engine.Tests.Core.Determinism;

public sealed class Fixed3DStateHashTests
{
    [Fact]
    public void IdenticalTransforms_ProduceIdenticalHashes()
    {
        var first = CreateTransform();
        var second = CreateTransform();

        Assert.Equal(Hash(first), Hash(second));
    }

    [Fact]
    public void PositionChange_ChangesHash()
    {
        var first = CreateTransform();
        var second = CreateTransform();

        second.Position = Vector(11, 20, 30);

        Assert.NotEqual(Hash(first), Hash(second));
    }

    [Fact]
    public void RotationChange_ChangesHash()
    {
        var first = CreateTransform();
        var second = CreateTransform();

        second.Rotation = FixedQuaternion.CreateFromAxisAngle(
            FixedVector3.UnitY,
            Fixed32.HalfPi);

        Assert.NotEqual(Hash(first), Hash(second));
    }

    [Fact]
    public void ScaleChange_ChangesHash()
    {
        var first = CreateTransform();
        var second = CreateTransform();

        second.Scale = Vector(2, 3, 4);

        Assert.NotEqual(Hash(first), Hash(second));
    }

    [Fact]
    public void FixedVector3_HashesAllComponents()
    {
        Assert.NotEqual(
            HashVector(Vector(1, 2, 3)),
            HashVector(Vector(1, 2, 4)));
    }

    [Fact]
    public void FixedQuaternion_HashesAllComponents()
    {
        var first = FixedQuaternion.Identity;

        var second = new FixedQuaternion(
            Fixed32.Zero,
            Fixed32.Zero,
            Fixed32.One,
            Fixed32.One);

        Assert.NotEqual(
            HashQuaternion(first),
            HashQuaternion(second));
    }

    private static FixedTransform3D CreateTransform()
    {
        return new FixedTransform3D(Vector(10, 20, 30));
    }

    private static FixedVector3 Vector(int x, int y, int z)
    {
        return new FixedVector3(
            Fixed32.FromInt(x),
            Fixed32.FromInt(y),
            Fixed32.FromInt(z));
    }

    private static DeterministicStateHash Hash(
        FixedTransform3D transform)
    {
        var hasher = DeterministicStateHasher.Create();
        transform.AddToHash(ref hasher);
        return hasher.GetHash();
    }

    private static DeterministicStateHash HashVector(
        FixedVector3 vector)
    {
        var hasher = DeterministicStateHasher.Create();
        hasher.AddFixedVector3(vector);
        return hasher.GetHash();
    }

    private static DeterministicStateHash HashQuaternion(
        FixedQuaternion quaternion)
    {
        var hasher = DeterministicStateHasher.Create();
        hasher.AddFixedQuaternion(quaternion);
        return hasher.GetHash();
    }
}
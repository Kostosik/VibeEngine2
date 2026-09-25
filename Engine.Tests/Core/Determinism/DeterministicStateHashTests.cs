using Engine.Core.Determinism;
using Engine.Core.Math;

namespace Engine.Tests.Core.Determinism;

public sealed class DeterministicStateHashTests
{
    [Fact]
    public void SameState_ProducesSameHash()
    {
        var first =
            DeterministicStateHasher.Create();

        first.AddInt32(100);
        first.AddUInt32(200);
        first.AddFixed32(
            Fixed32.FromInt(10));

        first.AddFixedVector2(
            new FixedVector2(
                Fixed32.FromInt(3),
                Fixed32.FromInt(4)));

        var second =
            DeterministicStateHasher.Create();

        second.AddInt32(100);
        second.AddUInt32(200);
        second.AddFixed32(
            Fixed32.FromInt(10));

        second.AddFixedVector2(
            new FixedVector2(
                Fixed32.FromInt(3),
                Fixed32.FromInt(4)));

        Assert.Equal(
            first.GetHash(),
            second.GetHash());
    }

    [Fact]
    public void DifferentState_ProducesDifferentHash()
    {
        var first =
            DeterministicStateHasher.Create();

        first.AddInt32(100);

        var second =
            DeterministicStateHasher.Create();

        second.AddInt32(101);

        Assert.NotEqual(
            first.GetHash(),
            second.GetHash());
    }

    [Fact]
    public void Order_IsPartOfState()
    {
        var first =
            DeterministicStateHasher.Create();

        first.AddInt32(1);
        first.AddInt32(2);

        var second =
            DeterministicStateHasher.Create();

        second.AddInt32(2);
        second.AddInt32(1);

        Assert.NotEqual(
            first.GetHash(),
            second.GetHash());
    }
}
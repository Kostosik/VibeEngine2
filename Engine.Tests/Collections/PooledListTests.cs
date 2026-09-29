using Engine.Memory.Collections;

namespace Engine.Tests.Collections;

public sealed class PooledListTests
{
    [Fact]
    public void AddConcurrent_ThrowsWhenLogicalCapacityIsExceeded()
    {
        using var list =
            new PooledList<int>(
                17);

        for (var i = 0;
             i < 17;
             i++)
        {
            list.Add(
                i);
        }

        Assert.Throws<InvalidOperationException>(
            () =>
                list.AddConcurrent(
                    17));

        Assert.Equal(
            17,
            list.Count);
    }

    [Fact]
    public void AddConcurrent_AppendsFromMultipleThreads()
    {
        const int count =
            10_000;

        using var list =
            new PooledList<int>(
                count);

        Parallel.For(
            0,
            count,
            index =>
                list.AddConcurrent(
                    index));

        Assert.Equal(
            count,
            list.Count);

        var values =
            new HashSet<int>();

        foreach (var value in
                 list.AsReadOnlySpan())
        {
            values.Add(
                value);
        }

        Assert.Equal(
            count,
            values.Count);
    }
}
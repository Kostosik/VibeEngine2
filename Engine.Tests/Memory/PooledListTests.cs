using Engine.Memory.Collections;

namespace Engine.Tests.Memory;

public sealed class PooledListTests
{
    [Fact]
    public void AddRange_FromOwnSpanPreservesValuesWhenGrowing()
    {
        using var list = new PooledList<int>(2);

        list.Add(10);
        list.Add(20);

        list.AddRange(list.AsReadOnlySpan());

        Assert.Equal(
            new[] { 10, 20, 10, 20 },
            list.AsSpan().ToArray());
    }
}
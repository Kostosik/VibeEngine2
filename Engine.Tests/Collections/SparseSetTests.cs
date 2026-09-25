using Engine.Core.Collections;

namespace Engine.Tests.Collections;

public sealed class SparseSetTests
{
    [Fact]
    public void NewSetIsEmpty()
    {
        var set =
            new SparseSet();

        Assert.Equal(
            0,
            set.Count);

        Assert.False(
            set.Contains(1));
    }

    [Fact]
    public void AddAddsValue()
    {
        var set =
            new SparseSet();

        var added =
            set.Add(42);

        Assert.True(added);
        Assert.Equal(1, set.Count);
        Assert.True(set.Contains(42));
    }

    [Fact]
    public void AddDuplicateReturnsFalse()
    {
        var set =
            new SparseSet();

        Assert.True(
            set.Add(42));

        Assert.False(
            set.Add(42));

        Assert.Equal(
            1,
            set.Count);
    }

    [Fact]
    public void RemoveRemovesValue()
    {
        var set =
            new SparseSet();

        set.Add(10);
        set.Add(20);

        var removed =
            set.Remove(10);

        Assert.True(removed);
        Assert.False(set.Contains(10));
        Assert.True(set.Contains(20));
        Assert.Equal(1, set.Count);
    }

    [Fact]
    public void RemoveMissingValueReturnsFalse()
    {
        var set =
            new SparseSet();

        Assert.False(
            set.Remove(42));
    }

    [Fact]
    public void RemoveUsesSwapWithLast()
    {
        var set =
            new SparseSet();

        set.Add(10);
        set.Add(20);
        set.Add(30);

        set.Remove(20);

        Assert.Equal(
            2,
            set.Count);

        Assert.True(
            set.Contains(10));

        Assert.True(
            set.Contains(30));

        Assert.Equal(
            30,
            set.Get(1));
    }

    [Fact]
    public void ClearRemovesAllValues()
    {
        var set =
            new SparseSet();

        set.Add(1);
        set.Add(2);
        set.Add(3);

        set.Clear();

        Assert.Equal(
            0,
            set.Count);

        Assert.False(
            set.Contains(1));

        Assert.False(
            set.Contains(2));

        Assert.False(
            set.Contains(3));
    }

    [Fact]
    public void ValuesCanExceedInitialCapacity()
    {
        var set =
            new SparseSet(2);

        Assert.True(
            set.Add(100));

        Assert.True(
            set.Contains(100));

        Assert.Equal(
            1,
            set.Count);
    }

    [Fact]
    public void ValuesAreAvailableThroughSpan()
    {
        var set =
            new SparseSet();

        set.Add(10);
        set.Add(20);
        set.Add(30);

        var span =
            set.AsReadOnlySpan();

        Assert.Equal(
            3,
            span.Length);

        Assert.Equal(
            10,
            span[0]);

        Assert.Equal(
            20,
            span[1]);

        Assert.Equal(
            30,
            span[2]);
    }

    [Fact]
    public void SpanContainsOnlyActiveValues()
    {
        var set =
            new SparseSet(16);

        set.Add(10);
        set.Add(20);

        var span =
            set.AsReadOnlySpan();

        Assert.Equal(
            set.Count,
            span.Length);

        Assert.Equal(
            2,
            span.Length);
    }

    [Fact]
    public void WritableSpanModifiesValue()
    {
        var set =
            new SparseSet();

        set.Add(10);

        var span =
            set.AsSpan();

        span[0] =
            42;

        Assert.Equal(
            42,
            set.Get(0));

        Assert.True(
            set.Contains(42) == false);
    }

    [Fact]
    public void GetRefReturnsReferenceToStoredValue()
    {
        var set =
            new SparseSet();

        set.Add(10);

        ref var value =
            ref set.GetRef(0);

        Assert.Equal(
            10,
            value);
    }

    [Fact]
    public void GetThrowsForInvalidIndex()
    {
        var set =
            new SparseSet();

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                set.Get(0);
            });
    }

    [Fact]
    public void GetRefThrowsForInvalidIndex()
    {
        var set =
            new SparseSet();

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                set.GetRef(0);
            });
    }

    [Fact]
    public void NegativeValueThrows()
    {
        var set =
            new SparseSet();

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                set.Add(-1);
            });
    }

    [Fact]
    public void ContainsNegativeValueReturnsFalse()
    {
        var set =
            new SparseSet();

        Assert.False(
            set.Contains(-1));
    }

    [Fact]
    public void ConstructorRejectsZeroCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                new SparseSet(0);
            });
    }

    [Fact]
    public void ConstructorRejectsNegativeCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                new SparseSet(-1);
            });
    }
}
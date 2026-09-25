namespace Engine.Core.Collections;

public sealed class SparseSet
{
    private int[] _sparse;
    private int[] _dense;
    private int _count;

    public int Count =>
        _count;

    public int Capacity =>
        _dense.Length;

    public SparseSet(
        int capacity = 16)
    {
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity));
        }

        _sparse =
            new int[capacity];

        _dense =
            new int[capacity];
    }

    public bool Contains(
        int value)
    {
        if (value < 0 ||
            value >= _sparse.Length)
        {
            return false;
        }

        var denseIndex =
            _sparse[value];

        return
            denseIndex >= 0 &&
            denseIndex < _count &&
            _dense[denseIndex] == value;
    }

    public bool Add(
        int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value));
        }

        EnsureSparseCapacity(value);

        if (Contains(value))
        {
            return false;
        }

        EnsureDenseCapacity();

        var denseIndex =
            _count;

        _dense[denseIndex] =
            value;

        _sparse[value] =
            denseIndex;

        _count++;

        return true;
    }

    public bool Remove(
        int value)
    {
        if (!Contains(value))
        {
            return false;
        }

        var denseIndex =
            _sparse[value];

        var lastIndex =
            _count - 1;

        var lastValue =
            _dense[lastIndex];

        if (denseIndex != lastIndex)
        {
            _dense[denseIndex] =
                lastValue;

            _sparse[lastValue] =
                denseIndex;
        }

        _count--;

        return true;
    }

    public int Get(
        int index)
    {
        ValidateIndex(index);

        return _dense[index];
    }

    public ref int GetRef(
        int index)
    {
        ValidateIndex(index);

        return ref _dense[index];
    }

    public Span<int> AsSpan()
    {
        return _dense.AsSpan(
            0,
            _count);
    }

    public ReadOnlySpan<int> AsReadOnlySpan()
    {
        return _dense.AsSpan(
            0,
            _count);
    }

    public void Clear()
    {
        _count = 0;
    }

    private void ValidateIndex(
        int index)
    {
        if ((uint)index >=
            (uint)_count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index));
        }
    }

    private void EnsureSparseCapacity(
        int value)
    {
        if (value < _sparse.Length)
        {
            return;
        }

        var newCapacity =
            _sparse.Length;

        while (newCapacity <= value)
        {
            newCapacity *= 2;
        }

        Array.Resize(
            ref _sparse,
            newCapacity);
    }

    private void EnsureDenseCapacity()
    {
        if (_count < _dense.Length)
        {
            return;
        }

        Array.Resize(
            ref _dense,
            _dense.Length * 2);
    }
}
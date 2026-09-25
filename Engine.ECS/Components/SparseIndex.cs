using Engine.Memory.Blocks;

namespace Engine.ECS.Components;

internal sealed class SparseIndex :
    IDisposable
{
    private const int PageShift = 10;
    private const int PageSize = 1 << PageShift;
    private const uint PageMask = PageSize - 1;

    private readonly Dictionary<
        uint,
        MemoryBlock<int>> _pages =
        new();

    public bool TryGet(
        uint key,
        out int value)
    {
        var pageId =
            key >> PageShift;

        if (!_pages.TryGetValue(
                pageId,
                out var page))
        {
            value = 0;
            return false;
        }

        var offset =
            (int)(key & PageMask);

        var encoded =
            page.Span[offset];

        if (encoded == 0)
        {
            value = 0;
            return false;
        }

        value =
            encoded - 1;

        return true;
    }

    public void Set(
        uint key,
        int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value));
        }

        var page =
            GetOrCreatePage(
                key);

        var offset =
            (int)(key & PageMask);

        page.Span[offset] =
            checked(value + 1);
    }

    public void Remove(
        uint key)
    {
        var pageId =
            key >> PageShift;

        if (!_pages.TryGetValue(
                pageId,
                out var page))
        {
            return;
        }

        var offset =
            (int)(key & PageMask);

        page.Span[offset] =
            0;
    }

    public void Clear()
    {
        foreach (var page in _pages.Values)
        {
            page.Span.Clear();
        }
    }

    public void Dispose()
    {
        foreach (var page in _pages.Values)
        {
            page.Dispose();
        }

        _pages.Clear();
    }

    private MemoryBlock<int> GetOrCreatePage(
        uint key)
    {
        var pageId =
            key >> PageShift;

        if (_pages.TryGetValue(
                pageId,
                out var existing))
        {
            return existing;
        }

        var page =
            new MemoryBlock<int>(
                PageSize);

        _pages.Add(
            pageId,
            page);

        return page;
    }
}
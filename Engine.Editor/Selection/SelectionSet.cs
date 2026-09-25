namespace Engine.Editor.Selection;

public sealed class SelectionSet<T>
    where T : notnull
{
    private readonly HashSet<T> _items = new();
    public event Action? Changed;
    public int Count =>
        _items.Count;

    public IReadOnlyCollection<T> Items =>
        _items;

    public bool Contains(
        T item)
    {
        return _items.Contains(item);
    }

    public bool Add(
        T item)
    {
        if (!_items.Add(item))
        {
            return false;
        }

        Changed?.Invoke();

        return true;
    }

    public bool Remove(
        T item)
    {
        if (!_items.Remove(item))
        {
            return false;
        }

        Changed?.Invoke();

        return true;
    }

    public void Clear()
    {
        if (_items.Count == 0)
        {
            return;
        }

        _items.Clear();

        Changed?.Invoke();
    }

    public void Set(
        T item)
    {
        if (_items.Count == 1 &&
            _items.Contains(item))
        {
            return;
        }

        _items.Clear();
        _items.Add(item);

        Changed?.Invoke();
    }
}
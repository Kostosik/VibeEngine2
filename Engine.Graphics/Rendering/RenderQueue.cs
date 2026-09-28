using Engine.Graphics.Commands;

namespace Engine.Graphics.Rendering;

public sealed class RenderQueue
{
    public readonly record struct Item(
        IRenderCommand Command,
        int Layer,
        long Order);

    private readonly List<Item> _items = new();

    private long _nextOrder;

    public int Count =>
        _items.Count;

    public IReadOnlyList<Item> Items =>
        _items;

    public void Submit(
        IRenderCommand command)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        _items.Add(
            new Item(
                command,
                command.Layer,
                _nextOrder++));
    }

    public void Sort()
    {
        _items.Sort(
            static (left, right) =>
            {
                var layerComparison =
                    left.Layer.CompareTo(
                        right.Layer);

                if (layerComparison != 0)
                {
                    return layerComparison;
                }

                return left.Order.CompareTo(
                    right.Order);
            });
    }

    public void Clear()
    {
        _items.Clear();

        _nextOrder = 0;
    }
}
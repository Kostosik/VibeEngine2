using Engine.Graphics.Commands;

namespace Engine.Graphics.Rendering;

public sealed class RenderQueue
{
    public readonly record struct Item
    {
        private readonly IRenderCommand? _command;
        private readonly int _worldTextureDataIndexPlusOne;

        public Item(
            IRenderCommand command,
            int layer,
            long order)
        {
            ArgumentNullException.ThrowIfNull(command);

            _command = command;
            _worldTextureDataIndexPlusOne = 0;
            Layer = layer;
            Order = order;
        }

        private Item(
            int worldTextureDataIndex,
            int layer,
            long order)
        {
            _command = null;
            _worldTextureDataIndexPlusOne =
                worldTextureDataIndex + 1;

            Layer = layer;
            Order = order;
        }

        public IRenderCommand Command =>
            _command ?? throw new InvalidOperationException(
                "This item contains world texture data, not a render command.");

        public bool HasWorldTextureData =>
            _worldTextureDataIndexPlusOne > 0;

        public int WorldTextureDataIndex =>
            _worldTextureDataIndexPlusOne - 1;

        public int Layer { get; }

        public long Order { get; }

        internal static Item FromWorldTextureData(
            int index,
            int layer,
            long order)
        {
            return new Item(index, layer, order);
        }

        public void Deconstruct(
            out IRenderCommand command,
            out int layer,
            out long order)
        {
            command = Command;
            layer = Layer;
            order = Order;
        }
    }

    private readonly List<Item> _items = new();

    private readonly List<DrawWorldTextureData>
        _worldTextureData = new();

    private long _nextOrder;

    public int Count => _items.Count;

    public IReadOnlyList<Item> Items => _items;

    public void Submit(IRenderCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        _items.Add(
            new Item(
                command,
                command.Layer,
                _nextOrder++));
    }

    public void Submit(DrawWorldTextureData command)
    {
        var index = _worldTextureData.Count;

        _worldTextureData.Add(command);

        _items.Add(
            Item.FromWorldTextureData(
                index,
                command.Layer,
                _nextOrder++));
    }

    internal DrawWorldTextureData GetWorldTextureData(
        int index)
    {
        return _worldTextureData[index];
    }

    public void Sort()
    {
        _items.Sort(
            static (left, right) =>
            {
                var layerComparison =
                    left.Layer.CompareTo(right.Layer);

                if (layerComparison != 0)
                {
                    return layerComparison;
                }

                return left.Order.CompareTo(right.Order);
            });
    }

    public void Clear()
    {
        _items.Clear();
        _worldTextureData.Clear();
        _nextOrder = 0;
    }
}
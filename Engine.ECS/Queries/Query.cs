using Engine.ECS.Components;
using Engine.ECS.Entities;

namespace Engine.ECS.Queries;

public readonly ref struct QueryItem<T>
    where T : struct
{
    private readonly ref T _component;

    public EntityId Entity { get; }

    public ref T Component =>
        ref _component;

    internal QueryItem(
        EntityId entity,
        ref T component)
    {
        Entity = entity;
        _component = ref component;
    }
}

public readonly struct Query<T>
    where T : struct
{
    private readonly ComponentStorage<T>? _storage;

    internal Query(
        ComponentStorage<T>? storage)
    {
        _storage = storage;
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(
            _storage);
    }

    public ref struct Enumerator
    {
        private readonly ComponentStorage<T>? _storage;
        private int _index;

        internal Enumerator(
            ComponentStorage<T>? storage)
        {
            _storage = storage;
            _index = -1;
        }

        public QueryItem<T> Current
        {
            get
            {
                var entity =
                    _storage!.GetEntity(_index);

                ref var component =
                    ref _storage.GetByIndex(
                        _index);

                return new QueryItem<T>(
                    entity,
                    ref component);
            }
        }

        public bool MoveNext()
        {
            if (_storage is null)
                return false;

            _index++;

            return _index < _storage.Count;
        }
    }
}
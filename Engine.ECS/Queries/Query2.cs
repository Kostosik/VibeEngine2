using Engine.ECS.Components;
using Engine.ECS.Entities;

namespace Engine.ECS.Queries;

public readonly ref struct QueryItem<T1, T2>
    where T1 : struct
    where T2 : struct
{
    private readonly ref T1 _first;
    private readonly ref T2 _second;

    public EntityId Entity { get; }

    public ref T1 First =>
        ref _first;

    public ref T2 Second =>
        ref _second;

    internal QueryItem(
        EntityId entity,
        ref T1 first,
        ref T2 second)
    {
        Entity = entity;
        _first = ref first;
        _second = ref second;
    }
}

public readonly struct Query<T1, T2>
    where T1 : struct
    where T2 : struct
{
    private readonly ComponentStorage<T1>? _first;
    private readonly ComponentStorage<T2>? _second;

    internal Query(
        ComponentStorage<T1>? first,
        ComponentStorage<T2>? second)
    {
        _first = first;
        _second = second;
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(
            _first,
            _second);
    }

    public ref struct Enumerator
    {
        private readonly ComponentStorage<T1>? _first;
        private readonly ComponentStorage<T2>? _second;
        private readonly bool _iterateFirst;

        private int _index;

        internal Enumerator(
            ComponentStorage<T1>? first,
            ComponentStorage<T2>? second)
        {
            _first = first;
            _second = second;

            _iterateFirst =
                first is not null &&
                (
                    second is null ||
                    first.Count <= second.Count);

            _index = -1;
        }

        public QueryItem<T1, T2> Current
        {
            get
            {
                if (_iterateFirst)
                {
                    var entity =
                        _first!.GetEntity(
                            _index);

                    ref var first =
                        ref _first.GetByIndex(
                            _index);

                    ref var second =
                        ref _second!.Get(
                            entity);

                    return new QueryItem<T1, T2>(
                        entity,
                        ref first,
                        ref second);
                }
                else
                {
                    var entity =
                        _second!.GetEntity(
                            _index);

                    ref var first =
                        ref _first!.Get(
                            entity);

                    ref var second =
                        ref _second.GetByIndex(
                            _index);

                    return new QueryItem<T1, T2>(
                        entity,
                        ref first,
                        ref second);
                }
            }
        }

        public bool MoveNext()
        {
            if (_first is null ||
                _second is null)
            {
                return false;
            }

            if (_iterateFirst)
            {
                _index++;

                return _index <
                       _first.Count;
            }

            _index++;

            return _index <
                   _second.Count;
        }
    }
}
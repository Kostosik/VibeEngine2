using Engine.ECS.Components;
using Engine.ECS.Entities;

namespace Engine.ECS.Queries;

public readonly ref struct ReadOnlyQueryItem<T>
    where T : struct
{
    private readonly ref readonly T _component;

    public EntityId Entity { get; }

    public ref readonly T Component =>
        ref _component;

    internal ReadOnlyQueryItem(
        EntityId entity,
        ref readonly T component)
    {
        Entity =
            entity;

        _component =
            ref component;
    }
}

public readonly struct ReadOnlyQuery<T>
    where T : struct
{
    private readonly ComponentStorage<T>? _storage;

    internal ReadOnlyQuery(
        ComponentStorage<T>? storage)
    {
        _storage =
            storage;
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
            _storage =
                storage;

            _index =
                -1;
        }

        public ReadOnlyQueryItem<T> Current
        {
            get
            {
                var entity =
                    _storage!.GetEntity(
                        _index);

                ref readonly var component =
                    ref _storage.GetByIndexReadOnly(
                        _index);

                return new ReadOnlyQueryItem<T>(
                    entity,
                    in component);
            }
        }

        public bool MoveNext()
        {
            if (_storage is null)
            {
                return false;
            }

            _index++;

            return _index <
                   _storage.Count;
        }
    }
}

public readonly ref struct ReadOnlyQueryItem<T1, T2>
    where T1 : struct
    where T2 : struct
{
    private readonly ref readonly T1 _first;
    private readonly ref readonly T2 _second;

    public EntityId Entity { get; }

    public ref readonly T1 First =>
        ref _first;

    public ref readonly T2 Second =>
        ref _second;

    internal ReadOnlyQueryItem(
        EntityId entity,
        ref readonly T1 first,
        ref readonly T2 second)
    {
        Entity =
            entity;

        _first =
            ref first;

        _second =
            ref second;
    }
}

public readonly struct ReadOnlyQuery<T1, T2>
    where T1 : struct
    where T2 : struct
{
    private readonly ComponentStorage<T1>? _first;
    private readonly ComponentStorage<T2>? _second;

    internal ReadOnlyQuery(
        ComponentStorage<T1>? first,
        ComponentStorage<T2>? second)
    {
        _first =
            first;

        _second =
            second;
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
            _first =
                first;

            _second =
                second;

            _iterateFirst =
                first is not null &&
                second is not null &&
                first.Count <= second.Count;

            _index =
                -1;
        }
        public ReadOnlyQueryItem<T1, T2> Current
        {
            get
            {
                if (_iterateFirst)
                {
                    var entity =
                        _first!.GetEntity(
                            _index);

                    ref readonly var firstComponentReadOnly =
                        ref _first.GetByIndexReadOnly(
                            _index);

                    ref readonly var secondComponentReadOnly =
                        ref _second!.GetReadOnly(
                            entity);

                    return new ReadOnlyQueryItem<T1, T2>(
                        entity,
                        in firstComponentReadOnly,
                        in secondComponentReadOnly);
                }

                var secondEntity =
                    _second!.GetEntity(
                        _index);

                ref readonly var secondComponentFromSecondStorage =
                    ref _second.GetByIndexReadOnly(
                        _index);

                ref readonly var firstComponentFromFirstStorage =
                    ref _first!.GetReadOnly(
                        secondEntity);

                return new ReadOnlyQueryItem<T1, T2>(
                    secondEntity,
                    in firstComponentFromFirstStorage,
                    in secondComponentFromSecondStorage);
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
                while (++_index <
                       _first.Count)
                {
                    var entity =
                        _first.GetEntity(
                            _index);

                    if (_second.Has(
                            entity))
                    {
                        return true;
                    }
                }

                return false;
            }

            while (++_index <
                   _second.Count)
            {
                var entity =
                    _second.GetEntity(
                        _index);

                if (_first.Has(
                        entity))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
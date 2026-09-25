using Engine.ECS;
using Engine.ECS.Entities;

namespace Engine.Worlds.Spatial;

public readonly ref struct SpatialQuery<T>
    where T : struct
{
    private readonly Engine.ECS.World _ecsWorld;
    private readonly ReadOnlySpan<EntityId> _entities;

    internal SpatialQuery(
        Engine.ECS.World ecsWorld,
        ReadOnlySpan<EntityId> entities)
    {
        _ecsWorld = ecsWorld;
        _entities = entities;
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(
            _ecsWorld,
            _entities);
    }

    public ref struct Enumerator
    {
        private readonly Engine.ECS.World _ecsWorld;
        private readonly ReadOnlySpan<EntityId> _entities;

        private int _index;
        private EntityId _entity;

        internal Enumerator(
            Engine.ECS.World ecsWorld,
            ReadOnlySpan<EntityId> entities)
        {
            _ecsWorld = ecsWorld;
            _entities = entities;

            _index = -1;
            _entity = EntityId.Invalid;
        }

        public SpatialQueryItem<T> Current
        {
            get
            {
                ref var component =
                    ref _ecsWorld.Get<T>(_entity);

                return new SpatialQueryItem<T>(
                    _entity,
                    ref component);
            }
        }

        public bool MoveNext()
        {
            while (++_index < _entities.Length)
            {
                var entity =
                    _entities[_index];

                if (!_ecsWorld.Exists(entity))
                {
                    continue;
                }

                if (!_ecsWorld.Has<T>(entity))
                {
                    continue;
                }

                _entity = entity;

                return true;
            }

            return false;
        }
    }
}
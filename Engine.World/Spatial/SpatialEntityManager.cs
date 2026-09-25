using Engine.ECS;
using Engine.ECS.Entities;
using Engine.Worlds;

namespace Engine.Worlds.Spatial;

public sealed class SpatialEntityManager
{
    private readonly World _world;
    private readonly Engine.ECS.World _ecsWorld;
    private readonly SpatialIndex _index;

    public SpatialEntityManager(
        World world,
        Engine.ECS.World ecsWorld)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(ecsWorld);

        _world = world;
        _ecsWorld = ecsWorld;
        _index = world.SpatialIndex;
        _ecsWorld.EntityDestroyed += OnEntityDestroyed;
    }

    public EntityId CreateEntity(
        WorldPosition position)
    {
        var entity =
            _ecsWorld.CreateEntity();

        try
        {
            SetPosition(
                entity,
                position);

            return entity;
        }
        catch
        {
            _ecsWorld.DestroyEntity(entity);
            throw;
        }
    }

    public bool DestroyEntity(
        EntityId entity)
    {
        if (!_ecsWorld.Exists(entity))
        {
            return false;
        }

        _index.RemoveEntity(entity);

        return _ecsWorld.DestroyEntity(entity);
    }

    public bool Contains(
        EntityId entity)
    {
        return _ecsWorld.Has<WorldPositionComponent>(
            entity);
    }

    public WorldPosition GetPosition(
        EntityId entity)
    {
        EnsureEntityExists(entity);

        if (!_ecsWorld.Has<WorldPositionComponent>(
                entity))
        {
            throw new KeyNotFoundException(
                $"Entity {entity.Index} does not have a world position.");
        }

        return _ecsWorld
            .Get<WorldPositionComponent>(entity)
            .Position;
    }

    public void SetPosition(
        EntityId entity,
        WorldPosition position)
    {
        EnsureEntityExists(entity);

        var newChunk =
            _world.GetChunkPosition(position);

        if (_ecsWorld.Has<WorldPositionComponent>(
                entity))
        {
            ref var component =
                ref _ecsWorld.Get<WorldPositionComponent>(
                    entity);

            if (component.Position == position)
            {
                return;
            }

            var previousChunk =
                _world.GetChunkPosition(
                    component.Position);

            component.Position =
                position;

            if (previousChunk != newChunk)
            {
                _index.Move(
                    entity,
                    previousChunk,
                    newChunk);
            }

            return;
        }

        _ecsWorld.Add(
            entity,
            new WorldPositionComponent(
                position));

        _index.Add(
            entity,
            newChunk);
    }

    public bool RemovePosition(
        EntityId entity)
    {
        EnsureEntityExists(entity);

        if (!_ecsWorld.Remove<WorldPositionComponent>(
                entity))
        {
            return false;
        }

        _index.RemoveEntity(entity);

        return true;
    }

    private void EnsureEntityExists(
        EntityId entity)
    {
        if (!_ecsWorld.Exists(entity))
        {
            throw new InvalidOperationException(
                $"Entity {entity.Index} does not exist.");
        }
    }

    private void OnEntityDestroyed(
    EntityId entity)
    {
        _index.RemoveEntity(entity);
    }
}
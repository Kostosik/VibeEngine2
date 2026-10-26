using Engine.ECS;
using Engine.ECS.Entities;
using Engine.Serialization.Binary;

namespace Engine.Networking.Replication;

public sealed class ReplicationEntityService
{
    private readonly ReplicationStateService _stateService;

    public ReplicationEntityService(
        ReplicationStateService stateService)
    {
        ArgumentNullException.ThrowIfNull(
            stateService);

        _stateService =
            stateService;
    }

    public EntityId Spawn(
        World world,
        NetworkEntityMap entityMap,
        ReplicatedEntityState state,
        SerializationContext context)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(entityMap);
        ArgumentNullException.ThrowIfNull(state);

        if (entityMap.TryGetEntity(state.Id, out var existingEntity))
        {
            if (!world.Exists(existingEntity))
            {
                entityMap.Unregister(existingEntity);
            }
            else
            {
                if (world.Has<NetworkIdentity>(existingEntity))
                {
                    var identity = world.Get<NetworkIdentity>(existingEntity);

                    if (identity.HasAssignedId && identity.Id != state.Id)
                    {
                        throw new InvalidOperationException(
                            $"Network identity mismatch for entity '{existingEntity}'.");
                    }

                    if (!identity.HasAssignedId)
                    {
                        world.Remove<NetworkIdentity>(existingEntity);
                        world.Add(existingEntity, new NetworkIdentity(state.Id));
                    }
                }
                else
                {
                    world.Add(existingEntity, new NetworkIdentity(state.Id));
                }

                _stateService.Apply(
                    world,
                    entityMap,
                    state,
                    context);

                return existingEntity;
            }
        }

        if (entityMap.TryGetEntity(
                state.Id,
                out _))
        {
            throw new InvalidOperationException(
                $"Network entity ID '{state.Id.Value}' is already mapped.");
        }

        var entity =
            world.CreateEntity();

        try
        {
            entityMap.Register(
                entity,
                state.Id);

            world.Add(
                entity,
                new NetworkIdentity(state.Id));

            _stateService.Apply(
                world,
                entityMap,
                state,
                context);

            return entity;
        }
        catch
        {
            entityMap.Unregister(
                entity);

            world.DestroyEntity(
                entity);

            throw;
        }
    }

    public void Update(
        World world,
        NetworkEntityMap entityMap,
        ReplicatedEntityState state,
        SerializationContext context)
    {
        _stateService.Apply(
            world,
            entityMap,
            state,
            context);
    }

    public bool Despawn(
        World world,
        NetworkEntityMap entityMap,
        NetworkEntityId networkId)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(entityMap);

        if (!entityMap.TryGetEntity(
                networkId,
                out var entity))
        {
            return false;
        }

        if (!world.Exists(entity))
        {
            entityMap.Unregister(
                entity);

            return false;
        }

        world.DestroyEntity(
            entity);

        entityMap.Unregister(
            entity);

        return true;
    }
}
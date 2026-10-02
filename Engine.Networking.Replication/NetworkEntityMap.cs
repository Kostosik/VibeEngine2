using Engine.ECS;
using Engine.ECS.Entities;

namespace Engine.Networking.Replication;

public sealed class NetworkEntityMap
{
    private readonly World _world;

    private readonly Dictionary<
        EntityId,
        NetworkEntityId> _networkIds =
        new();

    private readonly Dictionary<
        NetworkEntityId,
        EntityId> _entities =
        new();

    public NetworkEntityMap(
        World world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        _world =
            world;
    }

    public int Count =>
        _networkIds.Count;

    public void Register(
        EntityId entity,
        NetworkEntityId networkId)
    {
        if (!_world.Exists(
                entity))
        {
            throw new InvalidOperationException(
                $"Entity '{entity}' does not exist.");
        }

        if (!networkId.IsValid)
        {
            throw new ArgumentException(
                "Network entity ID must be valid.",
                nameof(networkId));
        }

        if (_networkIds.TryGetValue(
                entity,
                out var existingId))
        {
            if (existingId ==
                networkId)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Entity '{entity}' is already mapped to network ID '{existingId.Value}'.");
        }

        if (_entities.TryGetValue(
                networkId,
                out var existingEntity))
        {
            throw new InvalidOperationException(
                $"Network entity ID '{networkId.Value}' is already mapped to entity '{existingEntity}'.");
        }

        _networkIds.Add(
            entity,
            networkId);

        _entities.Add(
            networkId,
            entity);
    }

    public bool Unregister(
        EntityId entity)
    {
        if (!_networkIds.Remove(
                entity,
                out var networkId))
        {
            return false;
        }

        _entities.Remove(
            networkId);

        return true;
    }

    public bool TryGetNetworkId(
        EntityId entity,
        out NetworkEntityId networkId)
    {
        return _networkIds.TryGetValue(
            entity,
            out networkId);
    }

    public bool TryGetEntity(
        NetworkEntityId networkId,
        out EntityId entity)
    {
        return _entities.TryGetValue(
            networkId,
            out entity);
    }
}
using Engine.ECS;
using Engine.ECS.Entities;
using Engine.Networking.Authority;

namespace Engine.Networking.Replication;

/// <summary>
/// Keeps NetworkIdentity components and NetworkEntityMap consistent.
/// Call Reconcile regularly from the game update loop.
/// </summary>
public sealed class NetworkIdentityMapSynchronizer : IDisposable
{
    private readonly World _world;
    private readonly NetworkEntityMap _entityMap;
    private readonly INetworkAuthority _authority;

    private bool _disposed;

    public NetworkIdentityMapSynchronizer(
        World world,
        NetworkEntityMap entityMap,
        INetworkAuthority authority)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(entityMap);
        ArgumentNullException.ThrowIfNull(authority);

        _world = world;
        _entityMap = entityMap;
        _authority = authority;

        _world.EntityDestroyed += OnEntityDestroyed;
    }

    /// <summary>
    /// Reconciles local ECS entities with their network mappings.
    /// Returns the number of registrations, removals and ID assignments.
    /// </summary>
    public int Reconcile()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var changes = 0;

        // Remove mappings that no longer have a valid identity.
        foreach (var mapping in _entityMap.GetMappings())
        {
            if (!_world.Exists(mapping.Entity) ||
                !_world.Has<NetworkIdentity>(mapping.Entity))
            {
                if (_entityMap.Unregister(mapping.Entity))
                {
                    changes++;
                }

                continue;
            }

            var identity = _world.Get<NetworkIdentity>(
                mapping.Entity);

            if (!identity.HasAssignedId)
            {
                if (_entityMap.Unregister(mapping.Entity))
                {
                    changes++;
                }

                continue;
            }

            if (identity.Id != mapping.NetworkId)
            {
                throw new InvalidOperationException(
                    $"Network identity mismatch for entity '{mapping.Entity}'. " +
                    $"Component ID is '{identity.Id.Value}', " +
                    $"but map ID is '{mapping.NetworkId.Value}'.");
            }
        }

        var entities = _world.Inspector.GetEntities();

        // Validate all already-assigned IDs before adding new mappings.
        var seenIds = new Dictionary<NetworkEntityId, EntityId>();

        foreach (var entity in entities)
        {
            if (!_world.Has<NetworkIdentity>(entity))
            {
                continue;
            }

            var identity = _world.Get<NetworkIdentity>(entity);

            if (!identity.HasAssignedId)
            {
                continue;
            }

            if (seenIds.TryGetValue(
                    identity.Id,
                    out var otherEntity) &&
                otherEntity != entity)
            {
                throw new InvalidOperationException(
                    $"Network ID '{identity.Id.Value}' is assigned to " +
                    $"both entity '{otherEntity}' and entity '{entity}'.");
            }

            if (_entityMap.TryGetEntity(
                    identity.Id,
                    out var mappedEntity) &&
                mappedEntity != entity)
            {
                throw new InvalidOperationException(
                    $"Network ID '{identity.Id.Value}' is already mapped " +
                    $"to entity '{mappedEntity}'.");
            }

            seenIds[identity.Id] = entity;
        }

        var usedIds = seenIds.Keys.ToHashSet();

        foreach (var entity in entities)
        {
            if (!_world.Has<NetworkIdentity>(entity))
            {
                continue;
            }

            var identity = _world.Get<NetworkIdentity>(entity);

            // A client waits for the authoritative network ID.
            if (!identity.HasAssignedId)
            {
                if (!_authority.HasAuthority)
                {
                    continue;
                }

                do
                {
                    identity = NetworkIdentity.Create();
                }
                while (usedIds.Contains(identity.Id) ||
                       _entityMap.TryGetEntity(
                           identity.Id,
                           out _));

                usedIds.Add(identity.Id);

                // NetworkIdentity is a value-type component.
                // Replace the complete component with its assigned value.
                _world.Remove<NetworkIdentity>(entity);
                _world.Add(entity, identity);

                changes++;
            }

            if (_entityMap.TryGetNetworkId(
                    entity,
                    out var existingId))
            {
                if (existingId != identity.Id)
                {
                    throw new InvalidOperationException(
                        $"Network identity mismatch for entity '{entity}'.");
                }

                continue;
            }

            _entityMap.Register(
                entity,
                identity.Id);

            changes++;
        }

        return changes;
    }

    private void OnEntityDestroyed(EntityId entity)
    {
        _entityMap.Unregister(entity);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _world.EntityDestroyed -= OnEntityDestroyed;

        _disposed = true;
    }
}
using Engine.ECS;
using Engine.ECS.Entities;
using Engine.Serialization;
using Engine.Serialization.Binary;

namespace Engine.Networking.Replication;

public sealed class ReplicationStateService
{
    private readonly ReplicatedComponentRegistry _registry;

    public ReplicationStateService(
        ReplicatedComponentRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        _registry = registry;
    }

    public ReplicatedEntityState Capture(
        World world,
        NetworkEntityMap entityMap,
        EntityId entity,
        SerializationContext context)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(entityMap);

        if (!world.Exists(entity))
        {
            throw new InvalidOperationException(
                $"Entity '{entity}' does not exist.");
        }

        if (!entityMap.TryGetNetworkId(
                entity,
                out var networkId))
        {
            throw new InvalidOperationException(
                $"Entity '{entity}' is not mapped to a network entity ID.");
        }

        var components = new List<ReplicatedComponentState>();

        foreach (var componentType in world.Inspector.GetComponentTypes(entity))
        {
            if (!_registry.TryGetByType(
                    componentType,
                    out var entry))
            {
                continue;
            }

            var payload = entry.Capture(
                world,
                entity,
                context);

            components.Add(
                new ReplicatedComponentState(
                    entry.Id,
                    payload));
        }

        // Stable ordering is important for deterministic network state.
        components.Sort(
            static (left, right) =>
                string.CompareOrdinal(
                    left.Id,
                    right.Id));

        return new ReplicatedEntityState(
            networkId,
            components);
    }

    public void Apply(
     World world,
     NetworkEntityMap entityMap,
     ReplicatedEntityState state,
     SerializationContext context)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(entityMap);
        ArgumentNullException.ThrowIfNull(state);

        if (!entityMap.TryGetEntity(
                state.Id,
                out var entity))
        {
            throw new InvalidOperationException(
                $"Network entity ID '{state.Id.Value}' is not mapped to a local entity.");
        }

        if (!world.Exists(entity))
        {
            throw new InvalidOperationException(
                $"Mapped local entity '{entity}' does not exist.");
        }

        var entries =
            new Dictionary<
                string,
                ReplicatedComponentRegistry.Entry>(
                    StringComparer.Ordinal);

        foreach (var component in state.Components)
        {
            if (!entries.TryAdd(
                    component.Id,
                    null!))
            {
                throw new InvalidDataException(
                    $"Replicated component '{component.Id}' appears more than once.");
            }

            if (!_registry.TryGetById(
                    component.Id,
                    out var entry))
            {
                throw new InvalidOperationException(
                    $"Replicated component '{component.Id}' is not registered.");
            }

            entry.Validate(
                component.Payload.Span,
                context);

            entries[component.Id] =
                entry;
        }

        var incomingIds =
            entries.Keys.ToHashSet(
                StringComparer.Ordinal);

        foreach (var componentType in
                 world.Inspector.GetComponentTypes(entity))
        {
            if (!_registry.TryGetByType(
                    componentType,
                    out var entry))
            {
                continue;
            }

            if (!incomingIds.Contains(
                    entry.Id))
            {
                entry.Remove(
                    world,
                    entity);
            }
        }

        foreach (var component in state.Components)
        {
            entries[component.Id].Apply(
                world,
                entity,
                component.Payload.Span,
                context);
        }
    }
}
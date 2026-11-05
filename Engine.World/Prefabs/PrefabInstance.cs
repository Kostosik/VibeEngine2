using System.Collections.ObjectModel;
using Engine.ECS.Entities;
using Engine.ECS.Prefabs;

namespace Engine.Worlds.Prefabs;

/// <summary>
/// Runtime entities created from a prefab.
/// </summary>
public sealed class PrefabInstance
{
    internal PrefabInstance(
        IReadOnlyDictionary<PrefabEntityId, EntityId> entityMap,
        PrefabEntityId rootEntity)
    {
        ArgumentNullException.ThrowIfNull(entityMap);

        if (!entityMap.TryGetValue(rootEntity, out var root))
        {
            throw new ArgumentException(
                "Prefab root entity is missing from the instance.",
                nameof(entityMap));
        }

        EntityMap = new ReadOnlyDictionary<PrefabEntityId, EntityId>(
            new Dictionary<PrefabEntityId, EntityId>(entityMap));

        Root = root;
    }

    /// <summary>
    /// Runtime root entity.
    /// </summary>
    public EntityId Root { get; }

    /// <summary>
    /// Maps prefab-local IDs to runtime entity IDs.
    /// </summary>
    public IReadOnlyDictionary<PrefabEntityId, EntityId> EntityMap { get; }

    public EntityId GetEntity(PrefabEntityId prefabEntity)
    {
        if (!EntityMap.TryGetValue(prefabEntity, out var entity))
        {
            throw new KeyNotFoundException(
                $"Prefab entity '{prefabEntity.Value}' is not part of this instance.");
        }

        return entity;
    }
}
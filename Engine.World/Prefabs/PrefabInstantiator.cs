using Engine.ECS.Entities;
using Engine.ECS.Prefabs;
using Engine.Worlds.Spatial;

namespace Engine.Worlds.Prefabs;

/// <summary>
/// Creates runtime ECS entities from prefab assets.
/// </summary>
public static class PrefabInstantiator
{
    public static PrefabInstance Instantiate(
        World world,
        PrefabAsset prefab,
        WorldPosition? rootPosition = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(prefab);

        var rootPrefabEntity = prefab.Entities.Single(
            entity => entity.Id == prefab.RootEntity);

        var rootPositionComponent = rootPrefabEntity.Components
            .OfType<PrefabComponent<WorldPositionComponent>>()
            .SingleOrDefault();

        var hasRootPosition = rootPositionComponent is not null;

        var sourceRootPosition = hasRootPosition
            ? rootPositionComponent!.Component.Position
            : new WorldPosition(0, 0);

        // A supplied position moves all spatial entities by the same offset.
        // If the prefab root has no position, its source position is (0, 0).
        var offsetX = rootPosition.HasValue
            ? (long)rootPosition.Value.X - sourceRootPosition.X
            : 0L;

        var offsetY = rootPosition.HasValue
            ? (long)rootPosition.Value.Y - sourceRootPosition.Y
            : 0L;

        var entityMap = new Dictionary<PrefabEntityId, EntityId>(
            prefab.Entities.Count);

        var createdEntities = new List<EntityId>(
            prefab.Entities.Count);

        try
        {
            // Create every entity before adding components.
            // This allows references to point forward or sideways
            // within the prefab hierarchy.
            foreach (var prefabEntity in prefab.Entities)
            {
                var runtimeEntity = world.EcsWorld.CreateEntity();

                createdEntities.Add(runtimeEntity);
                entityMap.Add(prefabEntity.Id, runtimeEntity);
            }

            // Add components after the full mapping is available.
            foreach (var prefabEntity in prefab.Entities)
            {
                var runtimeEntity = entityMap[prefabEntity.Id];

                foreach (var component in prefabEntity.Components)
                {
                    var remappedComponent =
                        component.RemapEntityReferences(
                            reference => RemapReference(
                                reference,
                                entityMap));

                    if (remappedComponent.ComponentType ==
                        typeof(WorldPositionComponent))
                    {
                        var positionComponent =
                            (PrefabComponent<WorldPositionComponent>)
                                remappedComponent;

                        var sourcePosition =
                            positionComponent.Component.Position;

                        var adjustedPosition = rootPosition.HasValue
                            ? new WorldPosition(
                                checked((int)(sourcePosition.X + offsetX)),
                                checked((int)(sourcePosition.Y + offsetY)))
                            : sourcePosition;

                        // This also registers the entity in SpatialIndex.
                        world.SpatialEntities.SetPosition(
                            runtimeEntity,
                            adjustedPosition);

                        continue;
                    }

                    remappedComponent.AddToWorld(
                        world.EcsWorld,
                        runtimeEntity);
                }
            }

            // An explicit placement makes the root spatial even when
            // the source prefab did not contain WorldPositionComponent.
            if (rootPosition.HasValue && !hasRootPosition)
            {
                world.SpatialEntities.SetPosition(
                    entityMap[prefab.RootEntity],
                    rootPosition.Value);
            }

            return new PrefabInstance(
                entityMap,
                prefab.RootEntity);
        }
        catch
        {
            // DestroyEntity raises the event observed by SpatialEntityManager,
            // so any spatial-index entries created above are removed as well.
            for (var i = createdEntities.Count - 1; i >= 0; i--)
            {
                var entity = createdEntities[i];

                if (world.EcsWorld.Exists(entity))
                {
                    world.EcsWorld.DestroyEntity(entity);
                }
            }

            throw;
        }
    }

    private static EntityId RemapReference(
        EntityId reference,
        IReadOnlyDictionary<PrefabEntityId, EntityId> entityMap)
    {
        // Invalid references are allowed for optional component links.
        if (!reference.IsValid)
        {
            return EntityId.Invalid;
        }

        // Generation zero is reserved for prefab-local references.
        if (reference.Generation != 0)
        {
            throw new InvalidDataException(
                "Prefab contains a runtime entity reference.");
        }

        var prefabEntity = new PrefabEntityId(reference.Index);

        if (!entityMap.TryGetValue(
                prefabEntity,
                out var runtimeEntity))
        {
            throw new InvalidDataException(
                $"Prefab references missing entity '{prefabEntity.Value}'.");
        }

        return runtimeEntity;
    }
}
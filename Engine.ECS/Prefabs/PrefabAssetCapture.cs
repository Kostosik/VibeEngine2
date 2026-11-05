using Engine.ECS.Components;
using Engine.ECS.Entities;

namespace Engine.ECS.Prefabs;

/// <summary>
/// Captures entity hierarchies from an ECS World into prefab assets.
/// </summary>
public static class PrefabAssetCapture
{
    public static PrefabAsset Create(
        string name,
        World world,
        EntityId root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(world);

        if (!world.Exists(root))
        {
            throw new ArgumentException(
                "Prefab root entity does not exist in the world.",
                nameof(root));
        }

        var inspector = world.Inspector;

        // Collect the hierarchy relationships.
        var children = new Dictionary<
            EntityId,
            List<EntityId>>();

        foreach (var entity in inspector.GetEntities())
        {
            if (!inspector.TryGetComponent(
                    entity,
                    typeof(TransformParent2D),
                    out var boxedComponent))
            {
                continue;
            }

            var parent =
                ((TransformParent2D)boxedComponent!).Parent;

            if (!children.TryGetValue(
                    parent,
                    out var childList))
            {
                childList = new List<EntityId>();

                children.Add(
                    parent,
                    childList);
            }

            childList.Add(entity);
        }

        // Keep hierarchy capture deterministic.
        foreach (var childList in children.Values)
        {
            childList.Sort(
                static (left, right) =>
                {
                    var indexComparison =
                        left.Index.CompareTo(right.Index);

                    return indexComparison != 0
                        ? indexComparison
                        : left.Generation.CompareTo(right.Generation);
                });
        }

        // Collect root and all descendants.
        var orderedEntities = new List<EntityId>();
        var visited = new HashSet<EntityId>();
        var pending = new Stack<EntityId>();

        pending.Push(root);

        while (pending.Count > 0)
        {
            var current = pending.Pop();

            if (!visited.Add(current))
            {
                throw new InvalidDataException(
                    $"Prefab hierarchy contains a cycle at entity '{current.Index}'.");
            }

            orderedEntities.Add(current);

            if (!children.TryGetValue(
                    current,
                    out var childList))
            {
                continue;
            }

            for (var i = childList.Count - 1; i >= 0; i--)
            {
                pending.Push(childList[i]);
            }
        }

        // Assign stable local prefab IDs.
        var entityMap = new Dictionary<
            EntityId,
            PrefabEntityId>();

        for (var i = 0; i < orderedEntities.Count; i++)
        {
            entityMap.Add(
                orderedEntities[i],
                new PrefabEntityId(
                    checked((uint)i + 1)));
        }

        // Capture entity components.
        var prefabEntities = new List<PrefabEntity>(
            orderedEntities.Count);

        foreach (var runtimeEntity in orderedEntities)
        {
            var components = new List<PrefabComponent>();

            var componentTypes =
                inspector.GetComponentTypes(runtimeEntity);

            foreach (var componentType in componentTypes)
            {
                if (!inspector.TryGetComponent(
                        runtimeEntity,
                        componentType,
                        out var componentValue) ||
                    componentValue is null)
                {
                    throw new InvalidOperationException(
                        $"Could not read component '{componentType.FullName}' " +
                        $"from entity '{runtimeEntity.Index}'.");
                }

                var prefabComponent =
                    PrefabComponent.Create(
                        componentType,
                        componentValue);

                var remappedComponent =
                    prefabComponent.RemapEntityReferences(
                        reference =>
                        {
                            if (!reference.IsValid)
                            {
                                return EntityId.Invalid;
                            }

                            if (!entityMap.TryGetValue(
                                    reference,
                                    out var localEntity))
                            {
                                throw new InvalidDataException(
                                    $"Component '{componentType.FullName}' " +
                                    $"on entity '{runtimeEntity.Index}' " +
                                    "references an entity outside the prefab hierarchy.");
                            }

                            return localEntity.ToEntityReference();
                        });

                components.Add(remappedComponent);
            }

            prefabEntities.Add(
                new PrefabEntity(
                    entityMap[runtimeEntity],
                    components));
        }

        return new PrefabAsset(
            name,
            entityMap[root],
            prefabEntities);
    }
}
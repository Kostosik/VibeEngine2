namespace Engine.ECS.Prefabs;

/// <summary>
/// Describes a reusable ECS entity hierarchy.
/// </summary>
public sealed class PrefabAsset
{
    public PrefabAsset(
        string name,
        PrefabEntityId rootEntity,
        IReadOnlyList<PrefabEntity> entities)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(entities);

        if (!rootEntity.IsValid)
        {
            throw new ArgumentException(
                "Prefab root entity ID must be valid.",
                nameof(rootEntity));
        }

        if (entities.Count == 0)
        {
            throw new ArgumentException(
                "Prefab must contain at least one entity.",
                nameof(entities));
        }

        var entityIds = new HashSet<PrefabEntityId>();

        foreach (var entity in entities)
        {
            ArgumentNullException.ThrowIfNull(entity);

            if (!entityIds.Add(entity.Id))
            {
                throw new InvalidDataException(
                    $"Prefab contains duplicate entity ID '{entity.Id.Value}'.");
            }
        }

        if (!entityIds.Contains(rootEntity))
        {
            throw new InvalidDataException(
                "Prefab root entity does not exist.");
        }

        Entities = Array.AsReadOnly(entities.ToArray());

        ValidateEntityReferences(entityIds);

        Name = name;
        RootEntity = rootEntity;
    }

    public string Name { get; }

    public PrefabEntityId RootEntity { get; }

    public IReadOnlyList<PrefabEntity> Entities { get; }

    private void ValidateEntityReferences(
        HashSet<PrefabEntityId> entityIds)
    {
        foreach (var entity in Entities)
        {
            foreach (var component in entity.Components)
            {
                component.RemapEntityReferences(reference =>
                {
                    // Invalid EntityId can represent an optional reference.
                    if (!reference.IsValid)
                    {
                        return reference;
                    }

                    if (!PrefabEntityId.TryFromEntityReference(
                            reference,
                            out var prefabEntity))
                    {
                        throw new InvalidDataException(
                            $"Prefab component '{component.ComponentType.Name}' " +
                            "contains a reference to an external runtime entity.");
                    }

                    if (!entityIds.Contains(prefabEntity))
                    {
                        throw new InvalidDataException(
                            $"Prefab component '{component.ComponentType.Name}' " +
                            $"references missing prefab entity '{prefabEntity.Value}'.");
                    }

                    return reference;
                });
            }
        }
    }
}
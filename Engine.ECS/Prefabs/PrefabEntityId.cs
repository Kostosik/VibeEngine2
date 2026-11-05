using Engine.ECS.Entities;

namespace Engine.ECS.Prefabs;

/// <summary>
/// Identifies an entity inside a prefab asset.
/// </summary>
public readonly record struct PrefabEntityId(uint Value)
{
    public bool IsValid => Value != 0;

    public static PrefabEntityId Invalid => default;

    // Generation zero is reserved for prefab-local references.
    // Runtime EntityStore never creates entities with generation zero.
    internal EntityId ToEntityReference()
    {
        if (!IsValid)
        {
            throw new InvalidOperationException(
                "Prefab entity ID is invalid.");
        }

        return new EntityId(Value, 0);
    }

    internal static bool TryFromEntityReference(
        EntityId entity,
        out PrefabEntityId prefabEntity)
    {
        if (!entity.IsValid || entity.Generation != 0)
        {
            prefabEntity = Invalid;
            return false;
        }

        prefabEntity = new PrefabEntityId(entity.Index);
        return true;
    }
}
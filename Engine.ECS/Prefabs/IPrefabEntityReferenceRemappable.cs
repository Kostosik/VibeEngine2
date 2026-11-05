using Engine.ECS.Entities;

namespace Engine.ECS.Prefabs;

/// <summary>
/// Allows an ECS component to remap its entity references
/// when captured into or instantiated from a prefab.
/// </summary>
public interface IPrefabEntityReferenceRemappable
{
    /// <summary>
    /// Returns a copy of the component with remapped entity references.
    /// The returned object must have the same component type.
    /// </summary>
    object RemapPrefabEntityReferences(
        Func<EntityId, EntityId> remap);
}
using Engine.Core.Determinism;
using Engine.ECS.Entities;
using Engine.ECS.Prefabs;

namespace Engine.ECS.Components;

public struct TransformParent2D :
    IDeterministicState,
    IPrefabEntityReferenceRemappable
{
    public TransformParent2D(
        EntityId parent)
    {
        ValidateParent(parent);

        Parent = parent;
    }

    public object RemapPrefabEntityReferences(
    Func<EntityId, EntityId> remap)
    {
        ArgumentNullException.ThrowIfNull(remap);

        return new TransformParent2D(
            remap(Parent));
    }

    public EntityId Parent
    {
        readonly get => _parent;

        set
        {
            ValidateParent(value);

            _parent = value;
        }
    }

    private EntityId _parent;

    public void AddToHash(
        ref DeterministicStateHasher hasher)
    {
        hasher.AddUInt32(
            Parent.Index);

        hasher.AddUInt32(
            Parent.Generation);
    }

    private static void ValidateParent(
        EntityId parent)
    {
        if (!parent.IsValid)
        {
            throw new ArgumentException(
                "Parent entity must be valid.",
                nameof(parent));
        }
    }
}
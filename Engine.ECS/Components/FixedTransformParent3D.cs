using Engine.Core.Determinism;
using Engine.ECS.Entities;
using Engine.ECS.Prefabs;

namespace Engine.ECS.Components;

public struct FixedTransformParent3D :
    IDeterministicState,
    IPrefabEntityReferenceRemappable
{
    private EntityId _parent;

    public FixedTransformParent3D(EntityId parent)
    {
        ValidateParent(parent);
        _parent = parent;
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

    public object RemapPrefabEntityReferences(
        Func<EntityId, EntityId> remap)
    {
        ArgumentNullException.ThrowIfNull(remap);

        return new FixedTransformParent3D(remap(Parent));
    }

    public void AddToHash(ref DeterministicStateHasher hasher)
    {
        hasher.AddUInt32(Parent.Index);
        hasher.AddUInt32(Parent.Generation);
    }

    private static void ValidateParent(EntityId parent)
    {
        if (!parent.IsValid)
        {
            throw new ArgumentException(
                "Parent entity must be valid.",
                nameof(parent));
        }
    }
}
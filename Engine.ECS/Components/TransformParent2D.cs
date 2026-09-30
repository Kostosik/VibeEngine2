using Engine.Core.Determinism;
using Engine.ECS.Entities;

namespace Engine.ECS.Components;

public struct TransformParent2D :
    IDeterministicState
{
    public TransformParent2D(
        EntityId parent)
    {
        ValidateParent(parent);

        Parent = parent;
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
using Engine.Core.Determinism;
using Engine.ECS.Entities;

namespace Engine.ECS.Components;

public struct TransformParent2D :
    IDeterministicState
{
    public TransformParent2D(
        EntityId parent)
    {
        if (!parent.IsValid)
        {
            throw new ArgumentException(
                "Parent entity must be valid.",
                nameof(parent));
        }

        Parent = parent;
    }

    public EntityId Parent { get; set; }

    public void AddToHash(
        ref DeterministicStateHasher hasher)
    {
        hasher.AddUInt32(
            Parent.Index);

        hasher.AddUInt32(
            Parent.Generation);
    }
}
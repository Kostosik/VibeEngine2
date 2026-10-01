using Engine.Core.Determinism;
using Engine.Core.Math;
using Engine.ECS.Entities;

namespace Engine.Physics.Joints;

public struct DistanceJoint2D :
    IDeterministicState
{
    public DistanceJoint2D(
        EntityId first,
        EntityId second,
        Fixed32 length)
    {
        if (!first.IsValid)
        {
            throw new ArgumentException(
                "First entity must be valid.",
                nameof(first));
        }

        if (!second.IsValid)
        {
            throw new ArgumentException(
                "Second entity must be valid.",
                nameof(second));
        }

        if (first == second)
        {
            throw new ArgumentException(
                "Distance joint requires two different entities.",
                nameof(second));
        }

        if (length <= Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length),
                "Joint length must be greater than zero.");
        }

        First = first;
        Second = second;
        Length = length;
        Enabled = true;
    }

    public EntityId First { get; set; }

    public EntityId Second { get; set; }

    public Fixed32 Length { get; set; }

    public bool Enabled { get; set; }

    public void AddToHash(
        ref DeterministicStateHasher hasher)
    {
        hasher.AddUInt32(
            First.Index);

        hasher.AddUInt32(
            First.Generation);

        hasher.AddUInt32(
            Second.Index);

        hasher.AddUInt32(
            Second.Generation);

        hasher.AddFixed32(
            Length);

        hasher.AddBool(
            Enabled);
    }
}
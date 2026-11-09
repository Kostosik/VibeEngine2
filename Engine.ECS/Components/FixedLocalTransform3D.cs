using Engine.Core.Determinism;
using Engine.Core.Math;
using Engine.Transform;

namespace Engine.ECS.Components;

public struct FixedLocalTransform3D : IDeterministicState
{
    public FixedLocalTransform3D(FixedTransform3D value)
    {
        Value = value;
    }

    public FixedLocalTransform3D()
    {
        Value = FixedTransform3D.Identity;
    }

    public FixedTransform3D Value { get; set; }

    public FixedVector3 Position
    {
        readonly get => Value.Position;
        set
        {
            var transform = Value;
            transform.Position = value;
            Value = transform;
        }
    }

    public FixedQuaternion Rotation
    {
        readonly get => Value.Rotation;
        set
        {
            var transform = Value;
            transform.Rotation = value;
            Value = transform;
        }
    }

    public FixedVector3 Scale
    {
        readonly get => Value.Scale;
        set
        {
            var transform = Value;
            transform.Scale = value;
            Value = transform;
        }
    }

    public void AddToHash(ref DeterministicStateHasher hasher)
    {
        Value.AddToHash(ref hasher);
    }
}
using Engine.Core.Determinism;
using Engine.Core.Math;
using Engine.Transform;

namespace Engine.ECS.Components;

public struct LocalTransform2D :
    IDeterministicState
{
    public LocalTransform2D(
        FixedTransform2D value)
    {
        Value = value;
    }

    public FixedTransform2D Value { get; set; }

    public FixedVector2 Position
    {
        get => Value.Position;
        set
        {
            var transform = Value;
            transform.Position = value;
            Value = transform;
        }
    }

    public Fixed32 Rotation
    {
        get => Value.Rotation;
        set
        {
            var transform = Value;
            transform.Rotation = value;
            Value = transform;
        }
    }

    public FixedVector2 Scale
    {
        get => Value.Scale;
        set
        {
            var transform = Value;
            transform.Scale = value;
            Value = transform;
        }
    }

    public void AddToHash(
        ref DeterministicStateHasher hasher)
    {
        Value.AddToHash(
            ref hasher);
    }
}
using Engine.Core.Determinism;
using Engine.Core.Math;
using Engine.Transform;

namespace Engine.ECS.Components;

public struct WorldTransform2D :
    IDeterministicState
{
    private FixedTransform2D _value;

    public WorldTransform2D(
        FixedVector2 position)
    {
        _value = new FixedTransform2D(
            position);
    }

    public FixedVector2 Position
    {
        get => _value.Position;
        set => _value.Position = value;
    }

    public Fixed32 Rotation
    {
        get => _value.Rotation;
        set => _value.Rotation = value;
    }

    public FixedVector2 Scale
    {
        get => _value.Scale;
        set => _value.Scale = value;
    }

    public void AddToHash(
        ref DeterministicStateHasher hasher)
    {
        _value.AddToHash(
            ref hasher);
    }
}
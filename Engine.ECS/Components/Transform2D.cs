using Engine.Core.Determinism;
using Engine.Core.Math;

namespace Engine.ECS.Components;

public struct Transform2D :
    IDeterministicState
{
    public Transform2D(
        FixedVector2 position)
    {
        Position = position;
        Rotation = Fixed32.Zero;
        Scale = FixedVector2.One;
    }

    public FixedVector2 Position { get; set; }

    public Fixed32 Rotation { get; set; }

    public FixedVector2 Scale { get; set; }

    public void AddToHash(
        ref DeterministicStateHasher hasher)
    {
        hasher.AddFixedVector2(
            Position);

        hasher.AddFixed32(
            Rotation);

        hasher.AddFixedVector2(
            Scale);
    }
}
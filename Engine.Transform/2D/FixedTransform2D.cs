using Engine.Core.Determinism;
using Engine.Core.Math;

namespace Engine.Transform;

public struct FixedTransform2D :
    IDeterministicState
{
    public FixedTransform2D(
        FixedVector2 position)
    {
        Position = position;
        Rotation = Fixed32.Zero;
        Scale = new(
            Fixed32.One,
            Fixed32.One);
    }

    public FixedVector2 Position { get; set; }

    public Fixed32 Rotation { get; set; }

    public FixedVector2 Scale { get; set; }

    public static FixedTransform2D Identity =>
        new(FixedVector2.Zero);

    public void AddToHash(
        ref DeterministicStateHasher hasher)
    {
        hasher.AddFixedVector2(Position);
        hasher.AddFixed32(Rotation);
        hasher.AddFixedVector2(Scale);
    }
}
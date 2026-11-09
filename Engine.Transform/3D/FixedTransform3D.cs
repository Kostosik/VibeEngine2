using Engine.Core.Determinism;
using Engine.Core.Math;

namespace Engine.Transform;

public struct FixedTransform3D : IDeterministicState
{
    public FixedTransform3D(FixedVector3 position)
    {
        Position = position;
        Rotation = FixedQuaternion.Identity;
        Scale = FixedVector3.One;
    }

    public FixedVector3 Position { get; set; }

    public FixedQuaternion Rotation { get; set; }

    public FixedVector3 Scale { get; set; }

    public static FixedTransform3D Identity =>
        new(FixedVector3.Zero);

    public void AddToHash(ref DeterministicStateHasher hasher)
    {
        hasher.AddFixedVector3(Position);
        hasher.AddFixedQuaternion(Rotation);
        hasher.AddFixedVector3(Scale);
    }
}
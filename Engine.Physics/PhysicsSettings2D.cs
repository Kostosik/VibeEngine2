using Engine.Core.Determinism;
using Engine.Core.Math;

namespace Engine.Physics;

public sealed class PhysicsSettings2D :
    IDeterministicState
{
    public PhysicsSettings2D()
    {
        Gravity = FixedVector2.Zero;
        VelocityIterations = 4;
        PositionIterations = 2;
        PenetrationSlop = Fixed32.Zero;
        PositionCorrectionPercent = Fixed32.One;
    }

    public FixedVector2 Gravity { get; set; }

    public int VelocityIterations { get; set; }

    public int PositionIterations { get; set; }

    public Fixed32 PenetrationSlop { get; set; }

    public Fixed32 PositionCorrectionPercent { get; set; }

    public void AddToHash(
        ref DeterministicStateHasher hasher)
    {
        hasher.AddFixedVector2(Gravity);
        hasher.AddInt32(VelocityIterations);
        hasher.AddInt32(PositionIterations);
        hasher.AddFixed32(PenetrationSlop);
        hasher.AddFixed32(PositionCorrectionPercent);
    }
}
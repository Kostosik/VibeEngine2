using Engine.Core.Determinism;
using Engine.Core.Math;

namespace Engine.Physics.ThreeD;

public sealed class PhysicsSettings3D : IDeterministicState
{
    private int _velocityIterations;
    private int _positionIterations;
    private int _substeps;

    private Fixed32 _penetrationSlop;
    private Fixed32 _positionCorrectionPercent;

    private Fixed32 _sleepLinearVelocityThreshold;
    private Fixed32 _sleepAngularVelocityThreshold;
    private Fixed32 _sleepTime;

    public PhysicsSettings3D()
    {
        Gravity = new FixedVector3(
            Fixed32.Zero,
            Fixed32.FromInt(-10),
            Fixed32.Zero);

        VelocityIterations = 4;
        PositionIterations = 2;
        Substeps = 4;

        PenetrationSlop = Fixed32.FromRatio(1, 65536);
        PositionCorrectionPercent = Fixed32.One;

        SleepLinearVelocityThreshold =
            Fixed32.FromFloat(0.05f);

        SleepAngularVelocityThreshold =
            Fixed32.FromFloat(0.05f);

        SleepTime = Fixed32.FromFloat(0.5f);
    }

    public FixedVector3 Gravity { get; set; }

    public int VelocityIterations
    {
        get => _velocityIterations;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            _velocityIterations = value;
        }
    }

    public int PositionIterations
    {
        get => _positionIterations;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            _positionIterations = value;
        }
    }

    public int Substeps
    {
        get => _substeps;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            _substeps = value;
        }
    }

    public Fixed32 PenetrationSlop
    {
        get => _penetrationSlop;
        set
        {
            if (value < Fixed32.Zero)
                throw new ArgumentOutOfRangeException(nameof(value));

            _penetrationSlop = value;
        }
    }

    public Fixed32 PositionCorrectionPercent
    {
        get => _positionCorrectionPercent;
        set
        {
            if (value < Fixed32.Zero || value > Fixed32.One)
                throw new ArgumentOutOfRangeException(nameof(value));

            _positionCorrectionPercent = value;
        }
    }

    public Fixed32 SleepLinearVelocityThreshold
    {
        get => _sleepLinearVelocityThreshold;
        set
        {
            if (value < Fixed32.Zero)
                throw new ArgumentOutOfRangeException(nameof(value));

            _sleepLinearVelocityThreshold = value;
        }
    }

    public Fixed32 SleepAngularVelocityThreshold
    {
        get => _sleepAngularVelocityThreshold;
        set
        {
            if (value < Fixed32.Zero)
                throw new ArgumentOutOfRangeException(nameof(value));

            _sleepAngularVelocityThreshold = value;
        }
    }

    public Fixed32 SleepTime
    {
        get => _sleepTime;
        set
        {
            if (value <= Fixed32.Zero)
                throw new ArgumentOutOfRangeException(nameof(value));

            _sleepTime = value;
        }
    }

    public void AddToHash(ref DeterministicStateHasher hasher)
    {
        hasher.AddFixedVector3(Gravity);

        hasher.AddInt32(VelocityIterations);
        hasher.AddInt32(PositionIterations);
        hasher.AddInt32(Substeps);

        hasher.AddFixed32(PenetrationSlop);
        hasher.AddFixed32(PositionCorrectionPercent);

        hasher.AddFixed32(SleepLinearVelocityThreshold);
        hasher.AddFixed32(SleepAngularVelocityThreshold);
        hasher.AddFixed32(SleepTime);
    }
}
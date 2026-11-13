using Engine.Core.Determinism;
using Engine.Core.Math;

namespace Engine.Physics;

public sealed class PhysicsSettings2D :
    IDeterministicState
{
    private int _velocityIterations;
    private int _positionIterations;
    private Fixed32 _penetrationSlop;
    private Fixed32 _positionCorrectionPercent;
    private Fixed32 _sleepLinearVelocityThreshold;
    private Fixed32 _sleepAngularVelocityThreshold;
    private Fixed32 _sleepTime;
    private int _substeps;
    public PhysicsSettings2D()
    {
        Gravity = FixedVector2.Zero;
        VelocityIterations = 4;
        PositionIterations = 2;
        Substeps = 4;
        PenetrationSlop = Fixed32.FromRatio(
        1,
        65536);
        PositionCorrectionPercent = Fixed32.One;
        SleepLinearVelocityThreshold =
            Fixed32.FromFloat(0.05f);

        SleepAngularVelocityThreshold =
            Fixed32.FromFloat(0.05f);

        SleepTime =
            Fixed32.FromFloat(0.5f);
    }

    public FixedVector2 Gravity { get; set; }

    public Fixed32 SleepLinearVelocityThreshold
    {
        get => _sleepLinearVelocityThreshold;
        set
        {
            if (value < Fixed32.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Sleep linear velocity threshold cannot be negative.");
            }

            _sleepLinearVelocityThreshold = value;
        }
    }

    public int Substeps
    {
        get => _substeps;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Physics substeps must be greater than zero.");
            }

            _substeps = value;
        }
    }

    public Fixed32 SleepAngularVelocityThreshold
    {
        get => _sleepAngularVelocityThreshold;
        set
        {
            if (value < Fixed32.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Sleep angular velocity threshold cannot be negative.");
            }

            _sleepAngularVelocityThreshold = value;
        }
    }

    public Fixed32 SleepTime
    {
        get => _sleepTime;
        set
        {
            if (value <= Fixed32.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Sleep time must be greater than zero.");
            }

            _sleepTime = value;
        }
    }

    public int VelocityIterations
    {
        get => _velocityIterations;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Velocity iterations cannot be negative.");
            }

            _velocityIterations = value;
        }
    }

    public int PositionIterations
    {
        get => _positionIterations;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Position iterations cannot be negative.");
            }

            _positionIterations = value;
        }
    }

    public Fixed32 PenetrationSlop
    {
        get => _penetrationSlop;
        set
        {
            if (value < Fixed32.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Penetration slop cannot be negative.");
            }

            _penetrationSlop = value;
        }
    }

    public Fixed32 PositionCorrectionPercent
    {
        get => _positionCorrectionPercent;
        set
        {
            if (value < Fixed32.Zero ||
                value > Fixed32.One)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Position correction percent must be between zero and one.");
            }

            _positionCorrectionPercent = value;
        }
    }

    public void AddToHash(
        ref DeterministicStateHasher hasher)
    {
        hasher.AddFixedVector2(Gravity);
        hasher.AddInt32(VelocityIterations);
        hasher.AddInt32(
    Substeps);
        hasher.AddInt32(PositionIterations);
        hasher.AddFixed32(PenetrationSlop);
        hasher.AddFixed32(PositionCorrectionPercent);
        hasher.AddFixed32(
    SleepLinearVelocityThreshold);

        hasher.AddFixed32(
            SleepAngularVelocityThreshold);

        hasher.AddFixed32(
            SleepTime);
    }
}
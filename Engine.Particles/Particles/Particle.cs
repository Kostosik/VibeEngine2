using Engine.Core.Math;

namespace Engine.Particles.Particles;

public struct Particle
{
    public Particle(
        Vector3 position,
        Vector3 velocity,
        float lifetimeSeconds,
        float size = 1.0f,
        float rotation = 0.0f)
    {
        if (!float.IsFinite(lifetimeSeconds) || lifetimeSeconds <= 0.0f)
            throw new ArgumentOutOfRangeException(nameof(lifetimeSeconds));

        if (!float.IsFinite(size) || size < 0.0f)
            throw new ArgumentOutOfRangeException(nameof(size));

        if (!float.IsFinite(rotation))
            throw new ArgumentOutOfRangeException(nameof(rotation));

        Position = position;
        Velocity = velocity;
        AgeSeconds = 0.0f;
        LifetimeSeconds = lifetimeSeconds;
        Size = size;
        Rotation = rotation;
    }

    public Vector3 Position;
    public Vector3 Velocity;
    public float AgeSeconds;
    public float LifetimeSeconds;
    public float Size;
    public float Rotation;

    public bool IsAlive =>
        AgeSeconds < LifetimeSeconds;

    public float NormalizedAge
    {
        get
        {
            if (LifetimeSeconds <= 0.0f)
                return 1.0f;

            return Math.Clamp(
                AgeSeconds / LifetimeSeconds,
                0.0f,
                1.0f);
        }
    }
}
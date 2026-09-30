using Engine.Core.Math;
using Engine.Particles.Modifiers;

namespace Engine.Particles.Particles;

public sealed class ParticleSystem
{
    private readonly Particle[] _particles;
    private int _count;
    private readonly ParticleModifierSet _modifiers = new();
    public ParticleModifierSet Modifiers => _modifiers;
    public ParticleSystem(int maxParticles)
    {
        if (maxParticles <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxParticles));

        _particles = new Particle[maxParticles];
    }

    public int Capacity => _particles.Length;

    public int Count => _count;

    public bool IsFull =>
        _count >= _particles.Length;

    public ReadOnlySpan<Particle> Particles =>
        _particles.AsSpan(0, _count);

    public bool Spawn(in Particle particle)
    {
        if (_count >= _particles.Length)
            return false;

        _particles[_count++] = particle;
        return true;
    }

    public void Update(float deltaSeconds)
    {
        if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0.0f)
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

        var i = 0;

        while (i < _count)
        {
            ref var particle = ref _particles[i];

            particle.Position = new Vector3(
                particle.Position.X +
                    particle.Velocity.X * deltaSeconds,
                particle.Position.Y +
                    particle.Velocity.Y * deltaSeconds,
                particle.Position.Z +
                    particle.Velocity.Z * deltaSeconds);

            particle.AgeSeconds += deltaSeconds;

            _modifiers.Apply(
                ref particle,
                deltaSeconds);

            if (particle.AgeSeconds >= particle.LifetimeSeconds)
            {
                _count--;

                if (i < _count)
                    _particles[i] = _particles[_count];

                continue;
            }

            i++;
        }
    }

    public void Clear()
    {
        _count = 0;
    }
}
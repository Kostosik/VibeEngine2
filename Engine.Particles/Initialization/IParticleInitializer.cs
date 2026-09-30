using Engine.Core.Determinism;
using Engine.Particles.Particles;

namespace Engine.Particles.Initialization;

public interface IParticleInitializer
{
    void Initialize(
        ref Particle particle,
        ref DeterministicRandom random);
}
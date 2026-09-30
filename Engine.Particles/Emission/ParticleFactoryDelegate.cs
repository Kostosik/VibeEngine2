using Engine.Core.Determinism;
using Engine.Particles.Particles;

namespace Engine.Particles.Emission;

public delegate Particle ParticleFactoryDelegate(
    ref DeterministicRandom random);
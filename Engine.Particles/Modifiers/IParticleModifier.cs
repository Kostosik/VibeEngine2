using Engine.Particles.Particles;

namespace Engine.Particles.Modifiers;

public interface IParticleModifier
{
    void Modify(
        ref Particle particle,
        float deltaSeconds);
}
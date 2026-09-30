using Engine.Particles.Emission;

namespace Engine.Tests.Particles;

public sealed class ParticleEmitterTests
{
    [Fact]
    public void RateAccumulatesFractionalEmission()
    {
        var emitter = new ParticleEmitter(10.0f);

        Assert.Equal(0, emitter.Update(0.05f));
        Assert.Equal(1, emitter.Update(0.05f));
        Assert.Equal(0, emitter.Update(0.05f));
        Assert.Equal(1, emitter.Update(0.05f));
    }

    [Fact]
    public void EmitsWholeRateOverFullSecond()
    {
        var emitter = new ParticleEmitter(25.0f);

        Assert.Equal(25, emitter.Update(1.0f));
    }

    [Fact]
    public void BurstReturnsRequestedCount()
    {
        var emitter = new ParticleEmitter();

        Assert.Equal(20, emitter.Burst(20));
    }

    [Fact]
    public void ResetClearsAccumulatedEmission()
    {
        var emitter = new ParticleEmitter(10.0f);

        Assert.Equal(0, emitter.Update(0.05f));

        emitter.Reset();

        Assert.Equal(0, emitter.Update(0.05f));
    }
}
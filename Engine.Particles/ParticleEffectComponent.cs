namespace Engine.Particles;

public struct ParticleEffectComponent
{
    public ParticleEffectComponent()
    {
        EffectPath = string.Empty;
        InitialBurstCount = 0;
    }

    public string EffectPath { get; set; }

    public int InitialBurstCount { get; set; }
}
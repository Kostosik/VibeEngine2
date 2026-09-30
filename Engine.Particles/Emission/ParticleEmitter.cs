namespace Engine.Particles.Emission;

public sealed class ParticleEmitter
{
    private double _accumulator;
    private float _ratePerSecond;

    public ParticleEmitter(float ratePerSecond = 0.0f)
    {
        RatePerSecond = ratePerSecond;
    }

    public float RatePerSecond
    {
        get => _ratePerSecond;
        set
        {
            if (!float.IsFinite(value) || value < 0.0f)
                throw new ArgumentOutOfRangeException(nameof(value));

            _ratePerSecond = value;
        }
    }

    public int Update(float deltaSeconds)
    {
        if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0.0f)
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

        _accumulator += RatePerSecond * deltaSeconds;

        var count = (int)Math.Floor(_accumulator);

        _accumulator -= count;

        return count;
    }

    public int Burst(int count)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        return count;
    }

    public void Reset()
    {
        _accumulator = 0.0;
    }
}
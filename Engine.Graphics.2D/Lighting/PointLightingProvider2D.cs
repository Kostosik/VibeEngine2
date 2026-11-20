namespace Engine.Graphics2D.Lighting;

/// <summary>
/// Provides multiple point light contributions for a 2D scene.
/// Supports runtime modification of light sources.
/// </summary>
public sealed class PointLightingProvider2D :
    ILightingContributionProvider2D
{
    private readonly object _sync = new();

    private readonly List<Light2D> _lights = new();

    public LightingChannel2D SupportedChannels =>
        LightingChannel2D.Direct;

    /// <summary>
    /// Returns a stable snapshot of registered lights.
    /// </summary>
    public IReadOnlyList<Light2D> Lights
    {
        get
        {
            lock (_sync)
            {
                return Array.AsReadOnly(
                    _lights.ToArray());
            }
        }
    }

    public int Count
    {
        get
        {
            lock (_sync)
                return _lights.Count;
        }
    }

    public PointLightingProvider2D(
        IEnumerable<Light2D>? lights = null)
    {
        if (lights is not null)
            SetLights(lights);
    }

    /// <summary>
    /// Adds a new point light.
    /// </summary>
    public void AddLight(Light2D light)
    {
        lock (_sync)
        {
            _lights.Add(light);
        }
    }

    /// <summary>
    /// Replaces a light at the specified index.
    /// Useful for updating moving light sources.
    /// </summary>
    public void UpdateLight(
        int index,
        Light2D light)
    {
        lock (_sync)
        {
            if ((uint)index >= (uint)_lights.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index));
            }

            _lights[index] = light;
        }
    }

    /// <summary>
    /// Removes a light at the specified index.
    /// </summary>
    public void RemoveLight(int index)
    {
        lock (_sync)
        {
            if ((uint)index >= (uint)_lights.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index));
            }

            _lights.RemoveAt(index);
        }
    }

    /// <summary>
    /// Replaces the complete collection of point lights.
    /// </summary>
    public void SetLights(IEnumerable<Light2D> lights)
    {
        ArgumentNullException.ThrowIfNull(lights);

        var snapshot = lights.ToArray();

        lock (_sync)
        {
            _lights.Clear();
            _lights.AddRange(snapshot);
        }
    }

    /// <summary>
    /// Removes all point lights.
    /// </summary>
    public void Clear()
    {
        lock (_sync)
        {
            _lights.Clear();
        }
    }

    public void Contribute(
        in LightingContext2D context,
        LightingContributionWriter2D contributions)
    {
        ArgumentNullException.ThrowIfNull(contributions);

        Light2D[] snapshot;

        lock (_sync)
        {
            snapshot = _lights.ToArray();
        }

        foreach (var light in snapshot)
        {
            if (!light.IsEnabled)
                continue;

            contributions.Add(light);
        }
    }
}
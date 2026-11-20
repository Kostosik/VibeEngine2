namespace Engine.Graphics2D.Lighting;

/// <summary>
/// Provides ambient lighting contributions for a 2D scene.
/// </summary>
public sealed class AmbientLightingProvider2D :
    ILightingContributionProvider2D
{
    private readonly object _sync = new();

    private AmbientLight2D _light;

    public LightingChannel2D SupportedChannels =>
        LightingChannel2D.Ambient;

    /// <summary>
    /// Current ambient light configuration.
    /// Can be changed at runtime.
    /// </summary>
    public AmbientLight2D Light
    {
        get
        {
            lock (_sync)
                return _light;
        }
        set
        {
            lock (_sync)
                _light = value;
        }
    }

    public AmbientLightingProvider2D(
        AmbientLight2D light)
    {
        _light = light;
    }

    public void Contribute(
        in LightingContext2D context,
        LightingContributionWriter2D contributions)
    {
        ArgumentNullException.ThrowIfNull(contributions);

        var light = Light;

        if (!light.IsEnabled)
            return;

        contributions.Add(light);
    }
}
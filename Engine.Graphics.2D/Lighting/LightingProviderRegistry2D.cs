namespace Engine.Graphics2D.Lighting;

public readonly record struct LightingProviderRegistrySnapshot2D(
    long Revision,
    IReadOnlyList<ILightingContributionProvider2D> Providers);

/// <summary>
/// Thread-safe registry of lighting contribution providers.
/// Registration order defines contribution collection order.
/// </summary>
public sealed class LightingProviderRegistry2D
{
    private readonly object _sync = new();

    private readonly List<ILightingContributionProvider2D> _providers =
        new();

    private long _revision;

    public int Count
    {
        get
        {
            lock (_sync)
                return _providers.Count;
        }
    }

    public long Revision
    {
        get
        {
            lock (_sync)
                return _revision;
        }
    }

    public void Register(
        ILightingContributionProvider2D provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        if (!LightingChannelValidation2D.IsValidSupportedChannels(
                provider.SupportedChannels))
        {
            throw new ArgumentOutOfRangeException(
                nameof(provider),
                "Provider must support at least one valid lighting channel.");
        }

        lock (_sync)
        {
            if (_providers.Any(
                    existing => ReferenceEquals(existing, provider)))
            {
                throw new InvalidOperationException(
                    "This lighting provider is already registered.");
            }

            _providers.Add(provider);
            _revision++;
        }
    }

    public bool Unregister(
        ILightingContributionProvider2D provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        lock (_sync)
        {
            var index = _providers.FindIndex(
                existing => ReferenceEquals(existing, provider));

            if (index < 0)
                return false;

            _providers.RemoveAt(index);
            _revision++;

            return true;
        }
    }

    public LightingProviderRegistrySnapshot2D CaptureSnapshot()
    {
        lock (_sync)
        {
            return new LightingProviderRegistrySnapshot2D(
                _revision,
                Array.AsReadOnly(_providers.ToArray()));
        }
    }
}
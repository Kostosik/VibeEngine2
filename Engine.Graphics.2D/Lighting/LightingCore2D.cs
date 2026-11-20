namespace Engine.Graphics2D.Lighting;

/// <summary>
/// Collects lighting contributions from registered providers.
/// Does not depend on a specific lighting algorithm or graphics backend.
/// </summary>
public sealed class LightingCore2D
{
    private readonly LightingProviderRegistry2D _providers;

    public LightingProviderRegistry2D Providers => _providers;

    public LightingCore2D(
        LightingProviderRegistry2D? providers = null)
    {
        _providers = providers ?? new LightingProviderRegistry2D();
    }

    public LightingResult2D Collect(
        in LightingContext2D context)
    {
        var snapshot = _providers.CaptureSnapshot();

        var collected =
            new List<ILightingContribution2D>();

        foreach (var provider in snapshot.Providers)
        {
            var supportedChannels = provider.SupportedChannels;

            if (!LightingChannelValidation2D.IsValidSupportedChannels(
                    supportedChannels))
            {
                throw new InvalidOperationException(
                    $"Provider '{provider.GetType().Name}' " +
                    "declares invalid supported channels.");
            }

            var writer = new LightingContributionWriter2D(
                supportedChannels);

            try
            {
                provider.Contribute(
                    in context,
                    writer);
            }
            finally
            {
                writer.Seal();
            }

            collected.AddRange(writer.Contributions);
        }

        return collected.Count == 0
            ? LightingResult2D.Empty
            : new LightingResult2D(collected);
    }
}
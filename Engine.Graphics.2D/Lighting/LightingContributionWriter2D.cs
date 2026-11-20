namespace Engine.Graphics2D.Lighting;

/// <summary>
/// Collects contributions produced by one lighting provider.
/// </summary>
public sealed class LightingContributionWriter2D
{
    private readonly LightingChannel2D _supportedChannels;

    private readonly List<ILightingContribution2D> _contributions =
        new();

    private bool _sealed;

    internal LightingContributionWriter2D(
        LightingChannel2D supportedChannels)
    {
        _supportedChannels = supportedChannels;
    }

    public void Add(ILightingContribution2D contribution)
    {
        ObjectDisposedException.ThrowIf(_sealed, this);

        ArgumentNullException.ThrowIfNull(contribution);

        var channel = contribution.Channel;

        if (!LightingChannelValidation2D.IsSingleChannel(channel))
        {
            throw new ArgumentOutOfRangeException(
                nameof(contribution),
                "A contribution must specify exactly one valid lighting channel.");
        }

        if ((_supportedChannels & channel) == 0)
        {
            throw new InvalidOperationException(
                $"Provider does not support lighting channel '{channel}'.");
        }

        _contributions.Add(contribution);
    }

    internal IReadOnlyList<ILightingContribution2D> Contributions =>
        _contributions;

    internal void Seal()
    {
        _sealed = true;
    }
}
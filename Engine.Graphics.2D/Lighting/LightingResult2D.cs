namespace Engine.Graphics2D.Lighting;

/// <summary>
/// Contains collected lighting contributions separated by channel.
/// </summary>
public sealed class LightingResult2D
{
    public static LightingResult2D Empty { get; } =
        new(Array.Empty<ILightingContribution2D>());

    public IReadOnlyList<ILightingContribution2D> Contributions
    {
        get;
    }

    public IReadOnlyList<ILightingContribution2D> DirectLighting
    {
        get;
    }

    public IReadOnlyList<ILightingContribution2D> AmbientLighting
    {
        get;
    }

    public IReadOnlyList<ILightingContribution2D> IndirectLighting
    {
        get;
    }

    public IReadOnlyList<ILightingContribution2D> Emission
    {
        get;
    }

    public LightingChannel2D AvailableChannels { get; }

    internal LightingResult2D(
        IReadOnlyList<ILightingContribution2D> contributions)
    {
        ArgumentNullException.ThrowIfNull(contributions);

        var items = contributions.ToArray();

        Contributions = Array.AsReadOnly(items);

        DirectLighting = Filter(
            items,
            LightingChannel2D.Direct);

        AmbientLighting = Filter(
            items,
            LightingChannel2D.Ambient);

        IndirectLighting = Filter(
            items,
            LightingChannel2D.Indirect);

        Emission = Filter(
            items,
            LightingChannel2D.Emission);

        var channels = LightingChannel2D.None;

        foreach (var contribution in items)
        {
            channels |= contribution.Channel;
        }

        AvailableChannels = channels;
    }

    private static IReadOnlyList<ILightingContribution2D> Filter(
        ILightingContribution2D[] contributions,
        LightingChannel2D channel)
    {
        return Array.AsReadOnly(
            contributions
                .Where(item => item.Channel == channel)
                .ToArray());
    }
}
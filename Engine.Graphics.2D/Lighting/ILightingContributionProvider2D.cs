namespace Engine.Graphics2D.Lighting;

/// <summary>
/// Provides lighting contributions independently of the graphics backend.
/// </summary>
public interface ILightingContributionProvider2D
{
    LightingChannel2D SupportedChannels { get; }

    void Contribute(
        in LightingContext2D context,
        LightingContributionWriter2D contributions);
}
namespace Engine.Graphics2D.Lighting;

/// <summary>
/// Represents one backend-independent lighting contribution.
/// </summary>
public interface ILightingContribution2D
{
    LightingChannel2D Channel { get; }
}
namespace Engine.Graphics.Rendering.RenderGraph;

/// <summary>
/// Defines what happens to a color attachment before a pass writes to it.
/// </summary>
public enum RenderGraphLoadAction
{
    DontCare,
    Load,
    Clear
}

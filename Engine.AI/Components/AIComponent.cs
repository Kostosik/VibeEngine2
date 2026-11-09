namespace Engine.AI.Components;

/// <summary>
/// Marks an ECS entity as an AI-controlled agent.
/// </summary>
public struct AIComponent
{
    private bool _disabled;

    /// <summary>
    /// Determines whether AI processing is enabled.
    /// Agents are enabled by default.
    /// </summary>
    public bool Enabled
    {
        readonly get => !_disabled;
        set => _disabled = !value;
    }
}
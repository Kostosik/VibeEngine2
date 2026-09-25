namespace Engine.Tooling.DebugVisualization;

public sealed class DebugVisualizationService
{
    private readonly List<IDebugVisualizationProvider> _providers = new();

    public DebugVisualizationService(
        DebugDrawList drawList)
    {
        ArgumentNullException.ThrowIfNull(
            drawList);

        DrawList = drawList;
    }

    public DebugDrawList DrawList { get; }

    public IReadOnlyList<IDebugVisualizationProvider> Providers =>
        _providers;

    public void AddProvider(
        IDebugVisualizationProvider provider)
    {
        ArgumentNullException.ThrowIfNull(
            provider);

        if (_providers.Contains(
                provider))
        {
            return;
        }

        _providers.Add(
            provider);
    }

    public bool RemoveProvider(
        IDebugVisualizationProvider provider)
    {
        ArgumentNullException.ThrowIfNull(
            provider);

        return _providers.Remove(
            provider);
    }

    public void Build()
    {
        DrawList.Clear();

        foreach (var provider in _providers)
        {
            provider.Draw(
                DrawList);
        }
    }
}
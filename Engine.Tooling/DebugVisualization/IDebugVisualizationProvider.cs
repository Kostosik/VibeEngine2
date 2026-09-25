namespace Engine.Tooling.DebugVisualization;

public interface IDebugVisualizationProvider
{
    void Draw(
        DebugDrawList drawList);
}
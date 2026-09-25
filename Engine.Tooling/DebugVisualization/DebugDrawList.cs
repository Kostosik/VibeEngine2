namespace Engine.Tooling.DebugVisualization;

public sealed class DebugDrawList
{
    private readonly List<DebugLine> _lines = new();
    private readonly List<DebugRectangle> _rectangles = new();
    private readonly List<DebugCircle> _circles = new();

    public IReadOnlyList<DebugLine> Lines =>
        _lines;

    public IReadOnlyList<DebugRectangle> Rectangles =>
        _rectangles;

    public IReadOnlyList<DebugCircle> Circles =>
        _circles;

    public void DrawLine(
        DebugLine line)
    {
        _lines.Add(
            line);
    }

    public void DrawRectangle(
        DebugRectangle rectangle)
    {
        _rectangles.Add(
            rectangle);
    }

    public void DrawCircle(
        DebugCircle circle)
    {
        if (circle.Radius <= Engine.Core.Math.Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(circle),
                "Circle radius must be greater than zero.");
        }

        _circles.Add(
            circle);
    }

    public void Clear()
    {
        _lines.Clear();
        _rectangles.Clear();
        _circles.Clear();
    }
}
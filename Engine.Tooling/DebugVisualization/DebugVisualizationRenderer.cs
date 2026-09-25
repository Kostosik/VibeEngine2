using Engine.Core.Math;
using Engine.Graphics;
using Engine.Graphics.Commands;
using Engine.Graphics.Debug;

namespace Engine.Tooling.DebugVisualization;

public sealed class DebugVisualizationRenderer
{
    private readonly IGraphicsDevice _graphics;

    public DebugVisualizationRenderer(
        IGraphicsDevice graphics)
    {
        ArgumentNullException.ThrowIfNull(
            graphics);

        _graphics = graphics;
    }

    public void Render(
        DebugDrawList drawList)
    {
        ArgumentNullException.ThrowIfNull(
            drawList);

        foreach (var line in drawList.Lines)
        {
            _graphics.Submit(
                new DrawDebugLineCommand(
                    ToVector2(line.Start),
                    ToVector2(line.End),
                    ToGraphicsColor(line.Color),
                    DebugRenderSpace.World,
                    line.Layer));
        }

        foreach (var rectangle in drawList.Rectangles)
        {
            _graphics.Submit(
                new DrawDebugRectangleCommand(
                    ToVector2(rectangle.Bounds.Min),
                    ToVector2(rectangle.Bounds.Size),
                    ToGraphicsColor(rectangle.Color),
                    rectangle.Filled,
                    DebugRenderSpace.World,
                    rectangle.Layer));
        }

        foreach (var circle in drawList.Circles)
        {
            _graphics.Submit(
                new DrawDebugCircleCommand(
                    ToVector2(circle.Center),
                    circle.Radius.ToFloat(),
                    ToGraphicsColor(circle.Color),
                    circle.Filled,
                    16,
                    DebugRenderSpace.World,
                    circle.Layer));
        }
    }

    private static Vector2 ToVector2(
        FixedVector2 value)
    {
        return new Vector2(
            value.X.ToFloat(),
            value.Y.ToFloat());
    }

    private static Engine.Graphics.Debug.DebugColor ToGraphicsColor(
        DebugColor color)
    {
        return new Engine.Graphics.Debug.DebugColor(
            color.R,
            color.G,
            color.B,
            color.A);
    }
}
using Engine.Core.Math;

namespace Engine.Graphics2D.Rendering;

public readonly record struct CameraRenderTargetQuad2D(
    Vector2 TopLeft,
    Vector2 TopRight,
    Vector2 BottomRight,
    Vector2 BottomLeft)
{
    public const int VertexCount = 6;

    public (Vector2 Position, Vector2 UV) GetVertex(
        int index,
        Rectangle uv)
    {
        return index switch
        {
            0 => (TopLeft, new Vector2(uv.X, uv.Y)),

            1 => (TopRight, new Vector2(
                uv.X + uv.Width, uv.Y)),

            2 => (BottomRight, new Vector2(
                uv.X + uv.Width,
                uv.Y + uv.Height)),

            3 => (TopLeft, new Vector2(uv.X, uv.Y)),

            4 => (BottomRight, new Vector2(
                uv.X + uv.Width,
                uv.Y + uv.Height)),

            5 => (BottomLeft, new Vector2(
                uv.X, uv.Y + uv.Height)),

            _ => throw new ArgumentOutOfRangeException(
                nameof(index))
        };
    }
}
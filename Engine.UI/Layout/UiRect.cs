using Engine.Core.Math;

namespace Engine.UI.Layout;

public readonly record struct UiRect(
    float X,
    float Y,
    float Width,
    float Height)
{
    public float Right =>
        X + Width;

    public float Bottom =>
        Y + Height;

    public Vector2 Position =>
        new(X, Y);

    public Vector2 Size =>
        new(Width, Height);

    public UiRect Deflate(
        UiThickness thickness)
    {
        return new UiRect(
            X + thickness.Left,
            Y + thickness.Top,
            MathF.Max(
                0.0f,
                Width -
                thickness.Left -
                thickness.Right),
            MathF.Max(
                0.0f,
                Height -
                thickness.Top -
                thickness.Bottom));
    }

    public bool Contains(
    Vector2 point)
    {
        return point.X >= X &&
               point.X <= Right &&
               point.Y >= Y &&
               point.Y <= Bottom;
    }
}
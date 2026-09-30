using Engine.Core.Math;

namespace Engine.Transform;

public struct Transform2D
{
    public Transform2D(
        Vector2 position)
    {
        Position = position;
        Rotation = 0.0f;
        Scale = new Vector2(1.0f, 1.0f);
    }

    public Vector2 Position { get; set; }

    public float Rotation { get; set; }

    public Vector2 Scale { get; set; }

    public static Transform2D Identity =>
        new(Vector2.Zero);

    public readonly bool IsFinite =>
        float.IsFinite(Position.X) &&
        float.IsFinite(Position.Y) &&
        float.IsFinite(Rotation) &&
        float.IsFinite(Scale.X) &&
        float.IsFinite(Scale.Y);
}
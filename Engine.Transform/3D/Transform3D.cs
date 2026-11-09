using Engine.Core.Math;

namespace Engine.Transform;

public struct Transform3D
{
    public Transform3D(Vector3 position)
    {
        Position = position;
        Rotation = Quaternion.Identity;
        Scale = Vector3.One;
    }

    public Vector3 Position { get; set; }

    public Quaternion Rotation { get; set; }

    public Vector3 Scale { get; set; }

    public static Transform3D Identity =>
        new(Vector3.Zero);

    public readonly bool IsFinite =>
        float.IsFinite(Position.X) &&
        float.IsFinite(Position.Y) &&
        float.IsFinite(Position.Z) &&
        float.IsFinite(Rotation.X) &&
        float.IsFinite(Rotation.Y) &&
        float.IsFinite(Rotation.Z) &&
        float.IsFinite(Rotation.W) &&
        float.IsFinite(Scale.X) &&
        float.IsFinite(Scale.Y) &&
        float.IsFinite(Scale.Z);
}
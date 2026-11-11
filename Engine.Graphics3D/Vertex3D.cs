
using System.Runtime.InteropServices;
using Engine.Core.Math;

namespace Engine.Graphics3D;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct Vertex3D(
    Vector3 Position,
    Vector3 Normal,
    Vector2 TextureCoordinate)
{
    public static VertexLayout Layout { get; } = new(
        sizeof(float) * 8,
        new VertexAttributeDescription[]
        {
            new(
                VertexAttributeSemantic.Position,
                VertexAttributeFormat.Float3,
                0),

            new(
                VertexAttributeSemantic.Normal,
                VertexAttributeFormat.Float3,
                sizeof(float) * 3),

            new(
                VertexAttributeSemantic.TextureCoordinate,
                VertexAttributeFormat.Float2,
                sizeof(float) * 6)
        });

    public bool IsFinite =>
        float.IsFinite(Position.X) &&
        float.IsFinite(Position.Y) &&
        float.IsFinite(Position.Z) &&
        float.IsFinite(Normal.X) &&
        float.IsFinite(Normal.Y) &&
        float.IsFinite(Normal.Z) &&
        float.IsFinite(TextureCoordinate.X) &&
        float.IsFinite(TextureCoordinate.Y);
}

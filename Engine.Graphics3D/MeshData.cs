
namespace Engine.Graphics3D;

/// <summary>
/// Immutable indexed triangle mesh geometry.
/// Stores CPU-side vertex and index data.
/// </summary>
public sealed class MeshData
{
    private readonly Vertex3D[] _vertices;
    private readonly uint[] _indices;
    public VertexLayout Layout => Vertex3D.Layout;
    public MeshData(
        ReadOnlySpan<Vertex3D> vertices,
        ReadOnlySpan<uint> indices)
    {
        if (vertices.Length < 3)
        {
            throw new ArgumentException(
                "A mesh must contain at least three vertices.",
                nameof(vertices));
        }

        if (indices.Length < 3)
        {
            throw new ArgumentException(
                "A mesh must contain at least one triangle.",
                nameof(indices));
        }

        if (indices.Length % 3 != 0)
        {
            throw new ArgumentException(
                "Triangle index count must be divisible by three.",
                nameof(indices));
        }

        for (var i = 0; i < vertices.Length; i++)
        {
            if (!vertices[i].IsFinite)
            {
                throw new ArgumentException(
                    $"Vertex at index {i} contains non-finite values.",
                    nameof(vertices));
            }
        }

        for (var i = 0; i < indices.Length; i++)
        {
            if (indices[i] >= (uint)vertices.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(indices),
                    $"Index at position {i} references a missing vertex.");
            }
        }

        _vertices = vertices.ToArray();
        _indices = indices.ToArray();
    }

    public ReadOnlyMemory<Vertex3D> Vertices => _vertices;

    public ReadOnlyMemory<uint> Indices => _indices;

    public int VertexCount => _vertices.Length;

    public int IndexCount => _indices.Length;

    public int TriangleCount => _indices.Length / 3;
}

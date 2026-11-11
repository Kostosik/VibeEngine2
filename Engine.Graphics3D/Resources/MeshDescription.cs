
using Engine.Graphics.Resources;

namespace Engine.Graphics3D.Resources;

public readonly record struct MeshDescription
{
    public MeshDescription(
        int vertexCount,
        int indexCount,
        VertexLayout layout,
        GraphicsBufferUsage usage)
    {
        if (vertexCount < 3)
        {
            throw new ArgumentOutOfRangeException(
                nameof(vertexCount));
        }

        if (indexCount < 3)
        {
            throw new ArgumentOutOfRangeException(
                nameof(indexCount));
        }

        if (indexCount % 3 != 0)
        {
            throw new ArgumentException(
                "Index count must be divisible by three.",
                nameof(indexCount));
        }

        ArgumentNullException.ThrowIfNull(layout);

        if (!Enum.IsDefined(usage))
        {
            throw new ArgumentOutOfRangeException(
                nameof(usage),
                usage,
                "Unsupported graphics buffer usage.");
        }

        VertexCount = vertexCount;
        IndexCount = indexCount;
        Layout = layout;
        Usage = usage;
    }

    public int VertexCount { get; }

    public int IndexCount { get; }

    public int TriangleCount => IndexCount / 3;

    public VertexLayout Layout { get; }

    public GraphicsBufferUsage Usage { get; }
}


namespace Engine.Graphics3D;

public enum VertexAttributeSemantic
{
    Position,
    Normal,
    TextureCoordinate
}

public enum VertexAttributeFormat
{
    Float2,
    Float3,
    Float4
}

public readonly record struct VertexAttributeDescription
{
    public VertexAttributeDescription(
        VertexAttributeSemantic semantic,
        VertexAttributeFormat format,
        int offsetInBytes)
    {
        if (!Enum.IsDefined(semantic))
            throw new ArgumentOutOfRangeException(nameof(semantic));

        if (!Enum.IsDefined(format))
            throw new ArgumentOutOfRangeException(nameof(format));

        if (offsetInBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(offsetInBytes));

        Semantic = semantic;
        Format = format;
        OffsetInBytes = offsetInBytes;
    }

    public VertexAttributeSemantic Semantic { get; }

    public VertexAttributeFormat Format { get; }

    public int OffsetInBytes { get; }

    public int SizeInBytes => Format switch
    {
        VertexAttributeFormat.Float2 => sizeof(float) * 2,
        VertexAttributeFormat.Float3 => sizeof(float) * 3,
        VertexAttributeFormat.Float4 => sizeof(float) * 4,
        _ => throw new ArgumentOutOfRangeException(nameof(Format))
    };
}

public sealed class VertexLayout
{
    private readonly VertexAttributeDescription[] _attributes;

    public VertexLayout(
        int strideInBytes,
        ReadOnlySpan<VertexAttributeDescription> attributes)
    {
        if (strideInBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(strideInBytes));

        if (attributes.IsEmpty)
            throw new ArgumentException(
                "Vertex layout must contain at least one attribute.",
                nameof(attributes));

        _attributes = attributes.ToArray();

        var semantics = new HashSet<VertexAttributeSemantic>();

        foreach (var attribute in _attributes)
        {
            if (!Enum.IsDefined(attribute.Semantic) ||
                !Enum.IsDefined(attribute.Format))
            {
                throw new ArgumentException(
                    "Vertex layout contains an unsupported attribute.",
                    nameof(attributes));
            }

            if (attribute.OffsetInBytes < 0)
            {
                throw new ArgumentException(
                    "Attribute offset cannot be negative.",
                    nameof(attributes));
            }

            if (!semantics.Add(attribute.Semantic))
            {
                throw new ArgumentException(
                    $"Duplicate vertex semantic: {attribute.Semantic}.",
                    nameof(attributes));
            }

            var endOffset = (long)attribute.OffsetInBytes +
                            attribute.SizeInBytes;

            if (endOffset > strideInBytes)
            {
                throw new ArgumentException(
                    $"Attribute {attribute.Semantic} exceeds vertex stride.",
                    nameof(attributes));
            }
        }

        StrideInBytes = strideInBytes;
    }

    public int StrideInBytes { get; }

    public ReadOnlyMemory<VertexAttributeDescription> Attributes =>
        _attributes;
}

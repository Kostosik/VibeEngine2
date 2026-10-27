namespace Engine.Graphics.Resources;

public readonly record struct GraphicsBufferDescription
{
    public GraphicsBufferDescription(
        GraphicsBufferType type,
        int sizeInBytes,
        GraphicsBufferUsage usage)
    {
        if (sizeInBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sizeInBytes));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                "Unsupported graphics buffer type.");
        }

        if (!Enum.IsDefined(usage))
        {
            throw new ArgumentOutOfRangeException(
                nameof(usage),
                usage,
                "Unsupported graphics buffer usage.");
        }

        Type = type;
        SizeInBytes = sizeInBytes;
        Usage = usage;
    }

    public GraphicsBufferType Type { get; }

    public int SizeInBytes { get; }

    public GraphicsBufferUsage Usage { get; }
}
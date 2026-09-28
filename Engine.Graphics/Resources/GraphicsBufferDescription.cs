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

        Type = type;
        SizeInBytes = sizeInBytes;
        Usage = usage;
    }

    public GraphicsBufferType Type { get; }

    public int SizeInBytes { get; }

    public GraphicsBufferUsage Usage { get; }
}
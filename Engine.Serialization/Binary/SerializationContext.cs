namespace Engine.Serialization.Binary;

public readonly record struct SerializationContext
{
    public SerializationContext(
        int formatVersion = 1,
        int maxStringBytes = 1_048_576,
        int maxCollectionLength = 1_048_576,
        int maxPayloadBytes = 16_777_216)
    {
        if (formatVersion < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(formatVersion));
        }

        if (maxStringBytes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxStringBytes));
        }

        if (maxCollectionLength < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCollectionLength));
        }

        if (maxPayloadBytes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxPayloadBytes));
        }

        FormatVersion =
            formatVersion;

        MaxStringBytes =
            maxStringBytes;

        MaxCollectionLength =
            maxCollectionLength;

        MaxPayloadBytes =
            maxPayloadBytes;
    }

    public int FormatVersion { get; }

    public int MaxStringBytes { get; }

    public int MaxCollectionLength { get; }

    public int MaxPayloadBytes { get; }

    public static SerializationContext Default =>
        new SerializationContext(
            formatVersion: 1,
            maxStringBytes: 1_048_576,
            maxCollectionLength: 1_048_576,
            maxPayloadBytes: 16_777_216);
}
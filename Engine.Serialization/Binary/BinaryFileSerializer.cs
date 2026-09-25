namespace Engine.Serialization.Binary;

public static class BinaryFileSerializer
{
    public static void Save<T>(
        string path,
        T value,
        IBinarySerializer<T> serializer)
    {
        Save(
            path,
            value,
            serializer,
            SerializationContext.Default);
    }

    public static void Save<T>(
        string path,
        T value,
        IBinarySerializer<T> serializer,
        SerializationContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(serializer);

        var data =
            BinarySerializer.SerializeContainer(
                value,
                serializer,
                context);

        File.WriteAllBytes(
            path,
            data);
    }

    public static T Load<T>(
        string path,
        IBinarySerializer<T> serializer)
    {
        return Load(
            path,
            serializer,
            SerializationContext.Default);
    }

    public static T Load<T>(
        string path,
        IBinarySerializer<T> serializer,
        SerializationContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(serializer);

        var data =
            File.ReadAllBytes(
                path);

        return BinarySerializer.DeserializeContainer(
            data,
            serializer,
            context);
    }
}
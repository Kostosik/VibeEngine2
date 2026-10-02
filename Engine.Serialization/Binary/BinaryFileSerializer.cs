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

        var fullPath =
            Path.GetFullPath(path);

        var tempPath =
            $"{fullPath}.{Guid.NewGuid():N}.tmp";

        try
        {
            using (var stream =
                new FileStream(
                    tempPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
            {
                stream.Write(
                    data,
                    0,
                    data.Length);

                stream.Flush(
                    flushToDisk: true);
            }

            File.Move(
                tempPath,
                fullPath,
                overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // Cleanup failure must not mask the original save result.
                }
            }
        }
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
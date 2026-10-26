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
        ArgumentNullException.ThrowIfNull(context);

        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

        var maxFileSize =
            (long)context.MaxPayloadBytes +
            BinaryContainer.HeaderSize;

        if (stream.Length > maxFileSize)
        {
            throw new InvalidDataException(
                $"Serialized file size '{stream.Length}' exceeds " +
                $"the maximum allowed file size '{maxFileSize}'.");
        }

        if (stream.Length > Array.MaxLength)
        {
            throw new InvalidDataException(
                "Serialized file exceeds the maximum supported in-memory size.");
        }

        var data = new byte[checked((int)stream.Length)];

        stream.ReadExactly(data);

        return BinarySerializer.DeserializeContainer(
            data,
            serializer,
            context);
    }
}
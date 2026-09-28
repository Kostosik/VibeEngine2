using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;
using Engine.Serialization.Binary;

namespace Engine.Serialization.Content;

public sealed class BinaryContentImporter<T> :
    IContentImporter<T>
    where T : class
{
    private readonly IBinarySerializer<T> _serializer;
    private readonly SerializationContext _serializationContext;
    private readonly Func<ContentAsset, bool> _canImport;

    public BinaryContentImporter(
        IBinarySerializer<T> serializer,
        Func<ContentAsset, bool> canImport)
        : this(
            serializer,
            canImport,
            SerializationContext.Default)
    {
    }

    public BinaryContentImporter(
        IBinarySerializer<T> serializer,
        Func<ContentAsset, bool> canImport,
        SerializationContext serializationContext)
    {
        ArgumentNullException.ThrowIfNull(
            serializer);

        ArgumentNullException.ThrowIfNull(
            canImport);

        _serializer =
            serializer;

        _canImport =
            canImport;

        _serializationContext =
            serializationContext;
    }

    public bool CanImport(
        ContentAsset asset)
    {
        ArgumentNullException.ThrowIfNull(
            asset);

        return _canImport(
            asset);
    }

    public T Import(
        AssetPath path,
        ReadOnlyMemory<byte> data)
    {
        if (data.IsEmpty)
        {
            throw new InvalidDataException(
                $"Content asset '{path}' is empty.");
        }

        return BinarySerializer.DeserializeContainer(
            data.Span,
            _serializer,
            _serializationContext);
    }
}

public sealed class BinaryContentLoader<T> :
    IContentLoader<T>
    where T : class
{
    private readonly BinaryContentImporter<T> _importer;

    public BinaryContentLoader(
        IBinarySerializer<T> serializer,
        Func<ContentAsset, bool> canLoad)
        : this(
            serializer,
            canLoad,
            SerializationContext.Default)
    {
    }

    public async ValueTask<T> LoadAsync(
    AssetPath path,
    IContentLoadContext context,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        var data =
            await context.ReadBytesAsync(
                path,
                cancellationToken);

        return _importer.Import(
            path,
            data);
    }

    public BinaryContentLoader(
        IBinarySerializer<T> serializer,
        Func<ContentAsset, bool> canLoad,
        SerializationContext serializationContext)
    {
        _importer =
            new BinaryContentImporter<T>(
                serializer,
                canLoad,
                serializationContext);
    }

    public bool CanLoad(
        ContentAsset asset)
    {
        return _importer.CanImport(
            asset);
    }

    public Type AssetType =>
    typeof(T);

    object IContentLoader.Load(
        AssetPath path,
        IContentLoadContext context)
    {
        return Load(
            path,
            context);
    }

    public T Load(
        AssetPath path,
        IContentLoadContext context)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        return _importer.Import(
            path,
            context.ReadBytes(path));
    }
}
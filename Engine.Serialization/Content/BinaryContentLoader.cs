using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;
using Engine.Serialization.Binary;

namespace Engine.Serialization.Content;

public sealed class BinaryContentLoader<T> :
    IContentLoader<T>
    where T : class
{
    private readonly IBinarySerializer<T> _serializer;
    private readonly SerializationContext _serializationContext;
    private readonly Func<ContentAsset, bool> _canLoad;
    public BinaryContentLoader(
        IBinarySerializer<T> serializer,
        Func<ContentAsset, bool> canLoad)
        : this(
            serializer,
            canLoad,
            SerializationContext.Default)
    {
    }

    public BinaryContentLoader(
        IBinarySerializer<T> serializer,
        Func<ContentAsset, bool> canLoad,
        SerializationContext serializationContext)
    {
        ArgumentNullException.ThrowIfNull(
            serializer);

        ArgumentNullException.ThrowIfNull(
            canLoad);

        _serializer =
            serializer;

        _canLoad =
            canLoad;

        _serializationContext =
            serializationContext;
    }

    public bool CanLoad(
    ContentAsset asset)
    {
        return _canLoad(asset);
    }

    public T Load(
        AssetPath path,
        IContentLoadContext context)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        var data =
            context.ReadBytes(path);

        return BinarySerializer.DeserializeContainer(
            data.Span,
            _serializer,
            _serializationContext);
    }
}
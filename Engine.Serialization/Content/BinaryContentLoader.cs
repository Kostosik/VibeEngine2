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

    public BinaryContentLoader(
        IBinarySerializer<T> serializer)
        : this(
            serializer,
            SerializationContext.Default)
    {
    }

    public BinaryContentLoader(
        IBinarySerializer<T> serializer,
        SerializationContext serializationContext)
    {
        ArgumentNullException.ThrowIfNull(
            serializer);

        _serializer =
            serializer;

        _serializationContext =
            serializationContext;
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
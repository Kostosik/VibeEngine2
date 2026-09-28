namespace Engine.Content.Loading;

public sealed class ContentLoaderRegistry :
    IContentLoaderRegistry
{
    private readonly Dictionary<Type, object> _loaders =
        new();

    public void Register<T>(
        IContentLoader<T> loader)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(
            loader);

        var type =
            typeof(T);

        if (!_loaders.TryAdd(
                type,
                loader))
        {
            throw new InvalidOperationException(
                $"A content loader for '{type.Name}' is already registered.");
        }
    }

    public bool TryGet<T>(
        out IContentLoader<T>? loader)
        where T : class
    {
        if (_loaders.TryGetValue(
                typeof(T),
                out var value))
        {
            loader =
                (IContentLoader<T>)value;

            return true;
        }

        loader = null;

        return false;
    }

    public void Clear()
    {
        _loaders.Clear();
    }
}
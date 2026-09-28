using Engine.Content.Assets;
using Engine.Core.Assets;

namespace Engine.Content.Loading;

public sealed class ContentPipeline<TSource, TResult> :
    IContentLoader<TResult>
    where TSource : class
    where TResult : class
{
    private readonly IContentImporter<TSource> _importer;
    private readonly IContentProcessor<TSource, TResult> _processor;

    public ContentPipeline(
        IContentImporter<TSource> importer,
        IContentProcessor<TSource, TResult> processor)
    {
        ArgumentNullException.ThrowIfNull(
            importer);

        ArgumentNullException.ThrowIfNull(
            processor);

        _importer =
            importer;

        _processor =
            processor;
    }

    public Type AssetType =>
        typeof(TResult);

    public bool CanLoad(
        ContentAsset asset)
    {
        ArgumentNullException.ThrowIfNull(
            asset);

        return _importer.CanImport(
            asset);
    }

    public TResult Load(
        AssetPath path,
        IContentLoadContext context)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        var data =
            context.ReadBytes(
                path);

        var imported =
            _importer.Import(
                path,
                data);

        return _processor.Process(
            path,
            imported,
            context);
    }

    object IContentLoader.Load(
        AssetPath path,
        IContentLoadContext context)
    {
        return Load(
            path,
            context);
    }

    public async ValueTask<TResult> LoadAsync(
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

        cancellationToken.ThrowIfCancellationRequested();

        var imported =
            _importer.Import(
                path,
                data);

        return _processor.Process(
            path,
            imported,
            context);
    }
}

using Engine.Editor;
using Engine.Editor.Assets;
using Engine.Editor.Documents;
using Engine.Editor.Documents.Persistence;
using Engine.Editor.UI.Authoring;
using Engine.Worlds.Spatial;

namespace Engine.Editor.UI.Services;

internal readonly record struct EditorAssetCreationResult(
    string Path,
    string AssetRootPath,
    bool OpenDocument);

internal sealed class EditorAssetCreationWorkflow
{
    private readonly EditorContext _editor;
    private readonly IEditorDocumentPersistence? _documentPersistence;

    public EditorAssetCreationWorkflow(
        EditorContext editor,
        IEditorDocumentPersistence? documentPersistence)
    {
        ArgumentNullException.ThrowIfNull(editor);

        _editor = editor;
        _documentPersistence = documentPersistence;
    }

    public EditorAssetCreationResult Create(
        EditorAssetCreationDefinition definition,
        string name,
        string? targetDirectory)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (_editor.ActiveGameProject is null)
        {
            throw new InvalidOperationException(
                "Open a game project before creating assets.");
        }

        var browser = _editor.AssetBrowser
            ?? throw new InvalidOperationException(
                "The Asset Browser is not initialized.");

        var service = new EditorAssetCreationService(browser);

        Action<string>? generatedFileWriter =
            definition.Id switch
            {
                "world-document" => WriteEmptyWorldAsset,

                "ui-document" => path =>
                    WriteUiDocumentAsset(name, path),

                _ => null
            };

        var path = service.Create(
            definition,
            name,
            targetDirectory,
            generatedFileWriter);

        var openDocument =
            definition.Id is
                "world-document" or
                "ui-document" or
                "animation-clip" or
                "sprite-animation";

        return new EditorAssetCreationResult(
            path,
            browser.RootPath,
            openDocument);
    }

    private void WriteEmptyWorldAsset(string path)
    {
        var persistence = _documentPersistence
            ?? throw new InvalidOperationException(
                "World document persistence is not configured.");

        using var ecsWorld = new Engine.ECS.World();

        using var world = new Engine.Worlds.World(
            new ChunkSize(32, 32),
            ecsWorld);

        using var document = new EditorDocument(world);

        persistence.Save(path, document);
    }

    private static void WriteUiDocumentAsset(
        string name,
        string path)
    {
        var document = new EditorUiDocument(name);

        EditorUiAssetFile.Save(document, path);
    }
}

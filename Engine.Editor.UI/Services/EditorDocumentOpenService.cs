
using Engine.Editor;
using Engine.Editor.Documents;
using Engine.Editor.Documents.Persistence;
using Engine.Editor.UI.Authoring;

namespace Engine.Editor.UI.Services;

internal sealed class EditorDocumentOpenService
{
    private readonly EditorContext _editor;
    private readonly IEditorDocumentFileService? _documentFiles;

    public EditorDocumentOpenService(
        EditorContext editor,
        IEditorDocumentFileService? documentFiles)
    {
        ArgumentNullException.ThrowIfNull(editor);

        _editor = editor;
        _documentFiles = documentFiles;
    }

    public static bool IsSupportedPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (path.EndsWith(
                ".sprite-animation.json",
                StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(
                ".animation.json",
                StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(
                ".particle.json",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".world" => true,
            ".ui" => true,
            _ => false
        };
    }

    public IEditorDocument Open(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalizedPath = Path.GetFullPath(path);

        var pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        var existingDocument =
            _editor.Session.AllDocuments.FirstOrDefault(
                document =>
                    document.FilePath is string documentPath &&
                    string.Equals(
                        Path.GetFullPath(documentPath),
                        normalizedPath,
                        pathComparison));

        if (existingDocument is not null)
        {
            _editor.Session.Activate(existingDocument);
            return existingDocument;
        }

        IEditorDocument document;

        if (normalizedPath.EndsWith(
                ".sprite-animation.json",
                StringComparison.OrdinalIgnoreCase))
        {
            document = SpriteAnimationDocument.Open(normalizedPath);
        }
        else if (normalizedPath.EndsWith(
                     ".animation.json",
                     StringComparison.OrdinalIgnoreCase))
        {
            document = AnimationClipDocument.Open(normalizedPath);
        }
        else if (normalizedPath.EndsWith(
                     ".particle.json",
                     StringComparison.OrdinalIgnoreCase))
        {
            document = ParticleEffectDocument.Open(normalizedPath);
        }
        else
        {
            switch (Path.GetExtension(normalizedPath).ToLowerInvariant())
            {
                case ".world":
                    {
                        var files = _documentFiles
                            ?? throw new InvalidOperationException(
                                "Document file service is not configured.");

                        // EditorContext сам открывает документ в сессии.
                        return _editor.OpenDocument(files, normalizedPath);
                    }

                case ".ui":
                    {
                        var uiDocument =
                            EditorUiAssetFile.Load(normalizedPath);

                        uiDocument.SetFilePath(normalizedPath);
                        document = uiDocument;
                        break;
                    }

                default:
                    throw new NotSupportedException(
                        $"Unsupported document type: " +
                        $"'{Path.GetExtension(normalizedPath)}'. " +
                        "Supported extensions are .world and .ui.");
            }
        }

        _editor.Session.Open(document);

        return document;
    }
}

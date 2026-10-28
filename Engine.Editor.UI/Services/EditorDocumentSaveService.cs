using Engine.Editor.Documents;
using Engine.Editor.Documents.Persistence;
using Engine.Editor.UI.Authoring;
using Engine.Editor.UI.Dialogs;

namespace Engine.Editor.UI.Services;

internal sealed class EditorDocumentSaveService
{
    private readonly IEditorDocumentFileService? _documentFiles;
    private readonly IEditorFileDialogService _fileDialogs;

    public EditorDocumentSaveService(
        IEditorDocumentFileService? documentFiles,
        IEditorFileDialogService fileDialogs)
    {
        ArgumentNullException.ThrowIfNull(fileDialogs);

        _documentFiles = documentFiles;
        _fileDialogs = fileDialogs;
    }

    public bool CanSave(IEditorDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return document is
            TilemapDocument or
            TileSetDocument or
            SpriteAnimationDocument or
            ParticleEffectDocument or
            AnimationClipDocument or
            ScriptDefinitionDocument or
            EditorUiDocument or
            EditorDocument;
    }

    public bool TrySave(IEditorDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        switch (document)
        {
            case TilemapDocument tilemap:
                tilemap.Save();
                return true;
            case TileSetDocument tileSet:
                tileSet.Save();
                return true;
            case SpriteAnimationDocument spriteAnimation:
                spriteAnimation.Save();
                return true;

            case ParticleEffectDocument particleEffect:
                particleEffect.Save();
                return true;

            case AnimationClipDocument animationClip:
                animationClip.Save();
                return true;

            case ScriptDefinitionDocument scriptDefinition:
                scriptDefinition.Save();
                return true;

            case EditorUiDocument uiDocument:
                return TrySaveUiDocument(uiDocument);

            case EditorDocument worldDocument:
                return TrySaveWorldDocument(worldDocument);

            default:
                throw new NotSupportedException(
                    $"Saving document type '{document.DocumentTypeId}' " +
                    "is not supported.");
        }
    }

    public bool TrySaveUiDocument(
        EditorUiDocument document,
        string? destinationPath = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        var path = destinationPath ?? document.FilePath;

        if (path is null)
        {
            path = _fileDialogs.ShowSaveUiFile();

            if (path is null)
            {
                return false;
            }
        }

        var normalizedPath = Path.GetFullPath(path);

        EditorUiAssetFile.Save(
            document,
            normalizedPath);

        document.SetFilePath(normalizedPath);
        document.MarkSaved();

        return true;
    }

    private bool TrySaveWorldDocument(
        EditorDocument document)
    {
        var files = _documentFiles
            ?? throw new InvalidOperationException(
                "Document file service is not configured.");

        if (document.FilePath is null)
        {
            var path = _fileDialogs.ShowSaveFile();

            if (path is null)
            {
                return false;
            }

            files.SaveAs(document, path);
        }
        else
        {
            files.Save(document);
        }

        return true;
    }
}
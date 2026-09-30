using Engine.Editor.Documents;

namespace Engine.Editor.Documents.Persistence;

public sealed class EditorDocumentFileService :
    IEditorDocumentFileService
{
    private readonly EditorContext _editor;
    private readonly IEditorDocumentPersistence _persistence;

    public EditorDocumentFileService(
        EditorContext editor,
        IEditorDocumentPersistence persistence)
    {
        ArgumentNullException.ThrowIfNull(
            editor);

        ArgumentNullException.ThrowIfNull(
            persistence);

        _editor =
            editor;

        _persistence =
            persistence;
    }

    public EditorDocument Open(
        string path)
    {
        var normalizedPath =
            NormalizePath(path);

        EnsurePathNotOpen(
            normalizedPath);

        return _editor.LoadDocument(
            _persistence,
            normalizedPath);
    }

    public void Save(
        EditorDocument document)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        var path =
            document.FilePath
            ?? throw new InvalidOperationException(
                "Document has no file path. Use SaveAs.");

        _editor.SaveDocument(
            document,
            _persistence,
            path);
    }

    public void SaveAs(
        EditorDocument document,
        string path)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        var normalizedPath =
            NormalizePath(path);

        EnsurePathNotOpen(
            normalizedPath,
            document);

        _editor.SaveDocument(
            document,
            _persistence,
            normalizedPath);
    }

    private void EnsurePathNotOpen(
        string path,
        EditorDocument? except = null)
    {
        foreach (var document in
                 _editor.Session.Documents)
        {
            if (ReferenceEquals(
                    document,
                    except))
            {
                continue;
            }

            if (document.FilePath is null)
            {
                continue;
            }

            if (PathsEqual(
                    document.FilePath,
                    path))
            {
                throw new InvalidOperationException(
                    $"Document '{path}' is already open.");
            }
        }
    }

    private static string NormalizePath(
        string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            path);

        return Path.GetFullPath(
            path);
    }

    private static bool PathsEqual(
        string left,
        string right)
    {
        var comparison =
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        return string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            comparison);
    }
}
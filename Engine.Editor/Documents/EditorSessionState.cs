namespace Engine.Editor.Documents;

public sealed class EditorSessionState
{
    public EditorSessionState(
        IReadOnlyList<string> documentPaths,
        string? activeDocumentPath)
    {
        ArgumentNullException.ThrowIfNull(
            documentPaths);

        DocumentPaths =
            documentPaths;

        ActiveDocumentPath =
            activeDocumentPath;
    }

    public IReadOnlyList<string> DocumentPaths { get; }

    public string? ActiveDocumentPath { get; }
}
namespace Engine.Editor.Documents;

public sealed class EditorSession
{
    private readonly List<EditorDocument> _documents = new();

    public IReadOnlyList<EditorDocument> Documents =>
        _documents;

    public EditorDocument? ActiveDocument { get; private set; }

    public EditorDocument Open(
        EditorDocument document)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        if (_documents.Contains(document))
        {
            throw new InvalidOperationException(
                "Document is already open.");
        }

        _documents.Add(document);
        ActiveDocument = document;

        return document;
    }

    public bool Close(
        EditorDocument document)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        if (!_documents.Remove(document))
        {
            return false;
        }

        if (ReferenceEquals(
                ActiveDocument,
                document))
        {
            ActiveDocument =
                _documents.Count > 0
                    ? _documents[^1]
                    : null;
        }

        return true;
    }

    public void Activate(
        EditorDocument document)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        if (!_documents.Contains(document))
        {
            throw new InvalidOperationException(
                "Document is not open.");
        }

        ActiveDocument = document;
    }

    public void CloseAll()
    {
        _documents.Clear();
        ActiveDocument = null;
    }
}
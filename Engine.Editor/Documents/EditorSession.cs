namespace Engine.Editor.Documents;

public sealed class EditorSession
{
    private readonly List<EditorDocument> _documents = new();

    public event Action<EditorDocument>? DocumentClosed;

    public IReadOnlyList<EditorDocument> Documents =>
        _documents;
    public event Action<EditorDocument?>? ActiveDocumentChanged;
    public EditorDocument? ActiveDocument { get; private set; }

    public EditorDocument Open(
        EditorDocument document)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        if (_documents.Contains(
                document))
        {
            throw new InvalidOperationException(
                "Document is already open.");
        }

        if (_documents.Any(
                existing =>
                    ReferenceEquals(
                        existing.World,
                        document.World)))
        {
            throw new InvalidOperationException(
                "A document for this World is already open.");
        }

        _documents.Add(
            document);

        ActiveDocument =
            document;
        ActiveDocumentChanged?.Invoke(
    ActiveDocument);
        return document;
    }

    public bool Close(
     EditorDocument document)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        if (document.IsDirty)
        {
            throw new InvalidOperationException(
                "Cannot close a dirty document. Save or discard its changes first.");
        }

        return CloseCore(
            document);
    }

    public bool Discard(
        EditorDocument document)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        return CloseCore(
            document);
    }

    private bool CloseCore(
        EditorDocument document)
    {
        if (!_documents.Remove(
                document))
        {
            return false;
        }

        var activeChanged =
            ReferenceEquals(
                ActiveDocument,
                document);

        if (activeChanged)
        {
            ActiveDocument =
                _documents.Count > 0
                    ? _documents[^1]
                    : null;
        }

        document.Dispose();

        DocumentClosed?.Invoke(
            document);

        if (activeChanged)
        {
            ActiveDocumentChanged?.Invoke(
                ActiveDocument);
        }

        return true;
    }

    public void Activate(
    EditorDocument document)
    {
        ArgumentNullException.ThrowIfNull(
            document);

        if (!_documents.Contains(
                document))
        {
            throw new InvalidOperationException(
                "Document is not open.");
        }

        if (ReferenceEquals(
                ActiveDocument,
                document))
        {
            return;
        }

        ActiveDocument =
            document;

        ActiveDocumentChanged?.Invoke(
            ActiveDocument);
    }

    public void CloseAll()
    {
        if (_documents.Any(
                static document =>
                    document.IsDirty))
        {
            throw new InvalidOperationException(
                "Cannot close dirty documents. Save or discard their changes first.");
        }

        CloseAllCore();
    }

    public void DiscardAll()
    {
        CloseAllCore();
    }

    private void CloseAllCore()
    {
        var documents =
            _documents.ToArray();

        _documents.Clear();

        ActiveDocument =
            null;

        foreach (var document in documents)
        {
            document.Dispose();

            DocumentClosed?.Invoke(
                document);
        }

        if (documents.Length > 0)
        {
            ActiveDocumentChanged?.Invoke(
                null);
        }
    }
}
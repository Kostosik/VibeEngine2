namespace Engine.Editor.Documents;

public sealed class EditorSession
{
    private readonly List<IEditorDocument> _allDocuments = new();
    private readonly List<EditorDocument> _worldDocuments = new();

    private IEditorDocument? _activeEditorDocument;

    // Совместимый API для существующих World-инструментов.
    public IReadOnlyList<EditorDocument> Documents =>
        _worldDocuments;

    public EditorDocument? ActiveDocument =>
        _activeEditorDocument as EditorDocument;

    // Общий API для документов всех типов.
    public IReadOnlyList<IEditorDocument> AllDocuments =>
        _allDocuments;

    public IEditorDocument? ActiveEditorDocument =>
        _activeEditorDocument;

    public event Action<EditorDocument>? DocumentClosed;

    public event Action<EditorDocument?>? ActiveDocumentChanged;

    public event Action<IEditorDocument>? AnyDocumentClosed;

    public event Action<IEditorDocument?>? ActiveEditorDocumentChanged;

    public EditorDocument Open(EditorDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        OpenCore(document);

        return document;
    }

    public IEditorDocument Open(IEditorDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        OpenCore(document);

        return document;
    }

    private void OpenCore(IEditorDocument document)
    {
        if (_allDocuments.Contains(document))
        {
            throw new InvalidOperationException(
                "Document is already open.");
        }

        if (document is EditorDocument worldDocument &&
            _worldDocuments.Any(existing =>
                ReferenceEquals(existing.World, worldDocument.World)))
        {
            throw new InvalidOperationException(
                "A document for this World is already open.");
        }

        _allDocuments.Add(document);

        if (document is EditorDocument world)
        {
            _worldDocuments.Add(world);
        }

        SetActiveDocument(document);
    }

    public bool Close(EditorDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return Close((IEditorDocument)document);
    }

    public bool Close(IEditorDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.IsDirty)
        {
            throw new InvalidOperationException(
                "Cannot close a dirty document. Save or discard its changes first.");
        }

        return CloseCore(document);
    }

    public bool Discard(EditorDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return Discard((IEditorDocument)document);
    }

    public bool Discard(IEditorDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return CloseCore(document);
    }

    public void Activate(EditorDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        Activate((IEditorDocument)document);
    }

    public void Activate(IEditorDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!_allDocuments.Contains(document))
        {
            throw new InvalidOperationException(
                "Document is not open.");
        }

        SetActiveDocument(document);
    }

    private void SetActiveDocument(IEditorDocument? document)
    {
        if (ReferenceEquals(_activeEditorDocument, document))
        {
            return;
        }

        var previousWorldDocument = ActiveDocument;

        _activeEditorDocument = document;

        var currentWorldDocument = ActiveDocument;

        ActiveEditorDocumentChanged?.Invoke(
            _activeEditorDocument);

        if (!ReferenceEquals(
                previousWorldDocument,
                currentWorldDocument))
        {
            ActiveDocumentChanged?.Invoke(
                currentWorldDocument);
        }
    }

    private bool CloseCore(IEditorDocument document)
    {
        if (!_allDocuments.Remove(document))
        {
            return false;
        }

        if (document is EditorDocument worldDocument)
        {
            _worldDocuments.Remove(worldDocument);
        }

        var activeChanged =
            ReferenceEquals(_activeEditorDocument, document);

        if (activeChanged)
        {
            _activeEditorDocument =
                _allDocuments.Count > 0
                    ? _allDocuments[^1]
                    : null;
        }

        if (document is IDisposable disposable)
        {
            disposable.Dispose();
        }

        AnyDocumentClosed?.Invoke(document);

        if (document is EditorDocument closedWorldDocument)
        {
            DocumentClosed?.Invoke(closedWorldDocument);
        }

        if (activeChanged)
        {
            ActiveEditorDocumentChanged?.Invoke(
                _activeEditorDocument);

            ActiveDocumentChanged?.Invoke(
                ActiveDocument);
        }

        return true;
    }

    public void CloseAll()
    {
        if (_allDocuments.Any(static document => document.IsDirty))
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
        var documents = _allDocuments.ToArray();

        _allDocuments.Clear();
        _worldDocuments.Clear();

        _activeEditorDocument = null;

        foreach (var document in documents)
        {
            if (document is IDisposable disposable)
            {
                disposable.Dispose();
            }

            AnyDocumentClosed?.Invoke(document);

            if (document is EditorDocument worldDocument)
            {
                DocumentClosed?.Invoke(worldDocument);
            }
        }

        if (documents.Length > 0)
        {
            ActiveEditorDocumentChanged?.Invoke(null);
            ActiveDocumentChanged?.Invoke(null);
        }
    }
}
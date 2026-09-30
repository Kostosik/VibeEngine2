using Engine.Core.Application;
using Engine.Core.Time;
using Engine.Editor;
using Engine.Editor.Documents;
using Engine.Editor.UI.Authoring;
using Engine.Editor.UI.Shell;
using Engine.Graphics.Resources;
using Engine.Input;
using Engine.UI.Core;
using Engine.Worlds;

namespace Engine.Editor.UI;

public sealed class EditorApplication :
    IApplication
{
    private readonly HashSet<EditorDocument> _subscribedDocuments = new();
    private bool _uiDirty = true;
    private readonly string _uiAssetPath;
    public EditorUiDocument UiDocument { get; }
    public EditorApplication(
     EditorContext editor,
     UiSystem ui,
     IInputBackend input,
     ITextureResourceManager assetPreviewTextures,
     EditorUiDocument uiDocument,
     string uiAssetPath)
    {
        ArgumentNullException.ThrowIfNull(
            editor);

        ArgumentNullException.ThrowIfNull(
            ui);

        ArgumentNullException.ThrowIfNull(
            input);

        ArgumentNullException.ThrowIfNull(
            assetPreviewTextures);

        ArgumentNullException.ThrowIfNull(
            uiDocument);

        Editor = editor;
        Ui = ui;
        Input = input;
        AssetPreviewTextures = assetPreviewTextures;
        UiDocument = uiDocument;

        UiHost =
            new EditorUiHost(
                editor,
                ui);

        _uiAssetPath = uiAssetPath;

        MainShell =
            new EditorMainShell(
                editor,
                assetPreviewTextures,
                uiDocument);

        MainShell.SaveRequested +=
    SaveUiDocument;
        Editor.Session.DocumentClosed +=
    UnsubscribeDocument;
        UiHost.Root.AddChild(
            MainShell);

        Editor.Session.ActiveDocumentChanged +=
    OnActiveDocumentChanged;

        Editor.Workspace.Changed +=
    MarkUiDirty;

        UiDocument.Changed +=
            MarkUiDirty;

        if (editor.AssetBrowser is not null)
        {
            editor.AssetBrowser.Changed +=
                MarkUiDirty;
        }

        
    }

    private void OnActiveDocumentChanged(
    EditorDocument? document)
    {
        MarkUiDirty();
    }

    private void SaveUiDocument()
    {
        EditorUiAssetFile.Save(
            UiDocument,
            _uiAssetPath);

        UiDocument.MarkSaved();

        MarkUiDirty();
    }

    public EditorContext Editor { get; }

    public UiSystem Ui { get; }

    public IInputBackend Input { get; }

    public ITextureResourceManager AssetPreviewTextures { get; }

    public EditorUiHost UiHost { get; }

    public EditorMainShell MainShell { get; }

    public EditorDocument OpenDocument(
        World world)
    {
        var document =
            Editor.OpenDocument(
                world);

        SubscribeDocument(
            document);

        MarkUiDirty();

        return document;
    }

    public void Initialize()
    {
        MarkUiDirty();
        RefreshIfNeeded();
    }

    public void Update(
        TimeSnapshot time)
    {
        Input.Update();

        RefreshIfNeeded();

        Ui.Update(
            time.Delta.TotalSeconds);
    }

    public void FixedUpdate(
        SimulationTime time)
    {
    }

    public void Render(
        double interpolationAlpha)
    {
        Ui.Render();
    }

    public void Shutdown()
    {
        Editor.Session.CloseAll();

        Editor.Session.DocumentClosed -=
            UnsubscribeDocument;

        Editor.Session.ActiveDocumentChanged -=
            OnActiveDocumentChanged;

        Editor.Workspace.Changed -=
    MarkUiDirty;

        UiDocument.Changed -=
            MarkUiDirty;

        if (Editor.AssetBrowser is not null)
        {
            Editor.AssetBrowser.Changed -=
                MarkUiDirty;
        }

        MainShell.SaveRequested -=
            SaveUiDocument;
    }

    private void SubscribeDocument(
        EditorDocument document)
    {
        if (!_subscribedDocuments.Add(
                document))
        {
            return;
        }

        document.EntitySelection.Changed +=
            MarkUiDirty;

        document.CommandHistory.Changed +=
            MarkUiDirty;
    }
    private void UnsubscribeDocument(
    EditorDocument document)
    {
        if (!_subscribedDocuments.Remove(
                document))
        {
            return;
        }

        document.EntitySelection.Changed -=
            MarkUiDirty;

        document.CommandHistory.Changed -=
            MarkUiDirty;

        MarkUiDirty();
    }
    private void MarkUiDirty()
    {
        _uiDirty = true;
    }

    private void RefreshIfNeeded()
    {
        if (!_uiDirty)
        {
            return;
        }

        _uiDirty = false;

        MainShell.Refresh();
    }
}
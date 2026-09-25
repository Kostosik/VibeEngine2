using Engine.Editor;
using Engine.Editor.UI.Panels;
using Engine.Editor.UI.Shell;
using Engine.UI.Core;
using Engine.UI.Layout;

namespace Engine.Editor.UI;

public sealed class EditorUiHost
{
    private readonly Dictionary<
        string,
        IEditorPanelViewFactory> _factories =
        new(StringComparer.Ordinal);


    private readonly Dictionary<
        string,
        UiWidget> _views =
        new(StringComparer.Ordinal);

    public EditorUiHost(
        EditorContext editor,
        UiSystem ui)
    {
        ArgumentNullException.ThrowIfNull(
            editor);

        ArgumentNullException.ThrowIfNull(
            ui);

        Editor = editor;
        Ui = ui;





        Root =
            new UiCanvas();

        ui.Root.AddChild(
            Root);
    }

    public EditorContext Editor { get; }

    public UiSystem Ui { get; }

    public UiCanvas Root { get; }

    public void RegisterFactory(
        IEditorPanelViewFactory factory)
    {
        ArgumentNullException.ThrowIfNull(
            factory);

        if (_factories.ContainsKey(
                factory.PanelId))
        {
            throw new InvalidOperationException(
                $"UI factory for panel '{factory.PanelId}' is already registered.");
        }

        _factories.Add(
            factory.PanelId,
            factory);
    }

    public void Refresh()
    {
        foreach (var panel in
                 Editor.Workspace.Panels)
        {
            if (!_factories.TryGetValue(
                    panel.Id,
                    out var factory))
            {
                continue;
            }

            if (!_views.TryGetValue(
                    panel.Id,
                    out var view))
            {
                view =
                    factory.Create();

                Root.AddChild(
                    view);

                _views.Add(
                    panel.Id,
                    view);
            }

            view.Visible =
                panel.IsOpen;
        }
    }
}
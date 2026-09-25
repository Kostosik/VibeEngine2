using Engine.Editor.Panels;
using Engine.UI.Core;

namespace Engine.Editor.UI.Panels;

public interface IEditorPanelViewFactory
{
    string PanelId { get; }

    UiWidget Create();
}
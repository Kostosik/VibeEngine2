using Engine.Editor.Documents;
using Engine.Editor.Inspection;
using Engine.UI.Controls;
using Engine.UI.Core;

namespace Engine.Editor.UI.Inspection;

public interface IInspectorPropertyEditor
{
    int Priority { get; }

    bool CanEdit(Type propertyType);

    UiWidget Create(
        EditorDocument document,
        EditorProperty property);
}
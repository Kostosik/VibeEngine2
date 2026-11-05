using Engine.Editor.Documents;
using Engine.Editor.Inspection;
using Engine.UI.Core;

namespace Engine.Editor.UI.Inspection;

public interface IPrefabInspectorPropertyEditor
{
    int Priority { get; }

    bool CanEdit(Type propertyType);

    UiWidget Create(
        PrefabDocument document,
        EditorProperty property);
}
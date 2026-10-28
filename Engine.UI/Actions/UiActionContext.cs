using Engine.UI.Assets;

namespace Engine.UI.Actions;

public enum UiActionTrigger
{
    Click,
    Submit,
    ValueChanged,
    SelectionChanged
}

public readonly record struct UiDropdownSelection(
    int SelectedIndex,
    string SelectedText);

public readonly record struct UiActionContext(
    Guid ElementId,
    string ElementName,
    UiAssetElementType ElementType,
    UiActionTrigger Trigger,
    object? Value);
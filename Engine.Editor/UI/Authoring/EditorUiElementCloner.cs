using Engine.Core.Math;

namespace Engine.Editor.UI.Authoring;

public static class EditorUiElementCloner
{
    public static EditorUiElement Clone(
        EditorUiElement source,
        Vector2 rootOffset = default,
        string rootNameSuffix = "")
    {
        ArgumentNullException.ThrowIfNull(source);

        return CloneRecursive(
            source,
            rootOffset,
            rootNameSuffix,
            isRoot: true);
    }

    private static EditorUiElement CloneRecursive(
        EditorUiElement source,
        Vector2 rootOffset,
        string rootNameSuffix,
        bool isRoot)
    {
        var layout = source.Layout;

        if (isRoot)
        {
            layout = layout with
            {
                Offset = layout.Offset + rootOffset
            };
        }

        var clone = new EditorUiElement(
            Guid.NewGuid(),
            source.Type,
            isRoot
                ? source.Name + rootNameSuffix
                : source.Name,
            layout)
        {
            Text = source.Text,
            Texture = source.Texture,
            PreserveAspectRatio = source.PreserveAspectRatio,
            Action = source.Action,
            Visible = source.Visible,
            Enabled = source.Enabled,
            ZIndex = source.ZIndex,
            DropdownOptions = source.DropdownOptions.ToArray(),
            SelectedIndex = source.SelectedIndex,
            ToggleValue = source.ToggleValue,
            Placeholder = source.Placeholder,
            MaxLength = source.MaxLength
        };

        foreach (var child in source.Children)
        {
            clone.AddChild(
                CloneRecursive(
                    child,
                    Vector2.Zero,
                    string.Empty,
                    isRoot: false),
                clone.Children.Count);
        }

        return clone;
    }
}
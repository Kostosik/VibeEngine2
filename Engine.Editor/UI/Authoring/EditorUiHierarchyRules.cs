namespace Engine.Editor.UI.Authoring;

internal static class EditorUiHierarchyRules
{
    public static bool CanContainChildren(
        EditorUiElementType type)
    {
        return type is
            EditorUiElementType.Root or
            EditorUiElementType.Panel or
            EditorUiElementType.ScrollView;
    }

    public static bool CanAcceptChild(
        EditorUiElement parent,
        EditorUiElementType childType)
    {
        ArgumentNullException.ThrowIfNull(parent);

        if (childType == EditorUiElementType.Root ||
            !CanContainChildren(parent.Type))
        {
            return false;
        }

        if (parent.Type == EditorUiElementType.ScrollView &&
            parent.Children.Count > 0)
        {
            return false;
        }

        return true;
    }

    public static void EnsureCanAcceptChild(
        EditorUiElement parent,
        EditorUiElementType childType)
    {
        if (!CanAcceptChild(parent, childType))
        {
            throw new InvalidOperationException(
                $"Element '{parent.Name}' ({parent.Type}) " +
                $"cannot contain a child of type '{childType}'.");
        }
    }
}
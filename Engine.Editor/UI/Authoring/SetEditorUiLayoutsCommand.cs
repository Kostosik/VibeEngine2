using Engine.Editor.Commands;

namespace Engine.Editor.UI.Authoring;

public sealed class SetEditorUiLayoutsCommand : IEditorCommand
{
    private readonly LayoutChange[] _changes;

    public SetEditorUiLayoutsCommand(
        EditorUiDocument document,
        IReadOnlyDictionary<Guid, EditorUiLayout> newLayouts)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(newLayouts);

        if (newLayouts.Count < 2)
        {
            throw new ArgumentException(
                "At least two elements are required.",
                nameof(newLayouts));
        }

        var changes = new List<LayoutChange>(
            newLayouts.Count);

        foreach (var pair in newLayouts)
        {
            var element = document.GetElement(pair.Key);

            if (ReferenceEquals(element, document.Root))
            {
                throw new ArgumentException(
                    "The root UI element cannot be transformed.",
                    nameof(newLayouts));
            }

            changes.Add(new LayoutChange(
                element,
                element.Layout,
                pair.Value));
        }

        _changes = changes.ToArray();
    }

    public void Execute()
    {
        foreach (var change in _changes)
        {
            change.Element.Layout = change.NewLayout;
        }
    }

    public void Undo()
    {
        foreach (var change in _changes)
        {
            change.Element.Layout = change.OldLayout;
        }
    }

    private readonly record struct LayoutChange(
        EditorUiElement Element,
        EditorUiLayout OldLayout,
        EditorUiLayout NewLayout);
}
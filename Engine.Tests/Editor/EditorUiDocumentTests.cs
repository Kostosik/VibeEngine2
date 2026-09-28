using Engine.Editor.UI.Authoring;

namespace Engine.Tests.Editor.Authoring;

public sealed class EditorUiDocumentTests
{
    [Fact]
    public void AddElement_CreatesHierarchy()
    {
        var document =
            new EditorUiDocument();

        var panel =
            document.AddElement(
                EditorUiElementType.Panel,
                document.Root.Id,
                "MainPanel");

        var button =
            document.AddElement(
                EditorUiElementType.Button,
                panel.Id,
                "PlayButton");

        Assert.Equal(
            document.Root,
            panel.Parent);

        Assert.Equal(
            panel,
            button.Parent);

        Assert.Single(
            panel.Children);

        Assert.True(
            document.ContainsElement(
                button.Id));
    }

    [Fact]
    public void RemoveAndUndo_RestoresEntireSubtree()
    {
        var document =
            new EditorUiDocument();

        var panel =
            document.AddElement(
                EditorUiElementType.Panel,
                document.Root.Id);

        var button =
            document.AddElement(
                EditorUiElementType.Button,
                panel.Id);

        document.Selection.Set(
            button.Id);

        Assert.True(
            document.RemoveElement(
                panel.Id));

        Assert.False(
            document.ContainsElement(
                panel.Id));

        Assert.False(
            document.ContainsElement(
                button.Id));

        Assert.Empty(
            document.Selection.Items);

        document.Undo();

        Assert.True(
            document.ContainsElement(
                panel.Id));

        Assert.True(
            document.ContainsElement(
                button.Id));

        Assert.Equal(
            panel,
            button.Parent);

        Assert.Contains(
            button.Id,
            document.Selection.Items);

        document.Redo();

        Assert.False(
            document.ContainsElement(
                panel.Id));

        Assert.False(
            document.ContainsElement(
                button.Id));
    }

    [Fact]
    public void Root_CannotBeRemovedOrCreated()
    {
        var document =
            new EditorUiDocument();

        Assert.False(
            document.RemoveElement(
                document.Root.Id));

        Assert.Throws<ArgumentException>(
            () =>
                document.AddElement(
                    EditorUiElementType.Root,
                    document.Root.Id));
    }

    [Fact]
    public void MarkSaved_TracksDocumentDirtyState()
    {
        var document =
            new EditorUiDocument();

        Assert.False(
            document.IsDirty);

        document.AddElement(
            EditorUiElementType.Label,
            document.Root.Id);

        Assert.True(
            document.IsDirty);

        document.MarkSaved();

        Assert.False(
            document.IsDirty);

        document.Undo();

        Assert.True(
            document.IsDirty);

        document.Redo();

        Assert.False(
            document.IsDirty);
    }
}
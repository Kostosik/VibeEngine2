using Engine.Core.Math;
using Engine.Editor.Commands;
using Engine.Editor.Documents;
using Engine.Editor.UI.Authoring;

namespace Engine.Tests.Editor.Authoring;

public sealed class EditorUiDocumentTests
{
    [Fact]
    public void AddElement_LeafCannotContainChildren()
    {
        var document = new EditorUiDocument();

        var button = document.AddElement(
            EditorUiElementType.Button,
            document.Root.Id);

        Assert.Throws<InvalidOperationException>(
            () => document.AddElement(
                EditorUiElementType.Label,
                button.Id));
    }

    [Fact]
    public void AddElement_ScrollViewCanContainOnlyOneChild()
    {
        var document = new EditorUiDocument();

        var scrollView = document.AddElement(
            EditorUiElementType.ScrollView,
            document.Root.Id);

        document.AddElement(
            EditorUiElementType.Panel,
            scrollView.Id);

        Assert.Throws<InvalidOperationException>(
            () => document.AddElement(
                EditorUiElementType.Label,
                scrollView.Id));
    }

    [Fact]
    public void Reparent_CannotMoveElementIntoLeaf()
    {
        var document = new EditorUiDocument();

        var source = document.AddElement(
            EditorUiElementType.Panel,
            document.Root.Id);

        var button = document.AddElement(
            EditorUiElementType.Button,
            document.Root.Id);

        var label = document.AddElement(
            EditorUiElementType.Label,
            source.Id);

        Assert.Throws<InvalidOperationException>(
            () => new ReparentEditorUiElementCommand(
                document,
                label.Id,
                button.Id));
    }

    [Fact]
    public void Reparent_IntoRotatedExpander_PreservesLocalTransform()
    {
        var document = new EditorUiDocument();

        var source = document.AddElement(
            EditorUiElementType.Panel,
            document.Root.Id);

        var expander = document.AddElement(
            EditorUiElementType.Expander,
            document.Root.Id);

        var button = document.AddElement(
            EditorUiElementType.Button,
            source.Id);

        document.Execute(
            new SetEditorUiLayoutCommand(
                document,
                source.Id,
                source.Layout,
                new EditorUiLayout(300, 100, 200, 200)));

        document.Execute(
            new SetEditorUiLayoutCommand(
                document,
                button.Id,
                button.Layout,
                new EditorUiLayout(20, 30, 40, 20)));

        document.Execute(
            new SetEditorUiLayoutCommand(
                document,
                expander.Id,
                expander.Layout,
                new EditorUiLayout(
                    Vector2.Zero,
                    new Vector2(100, 100),
                    new Vector2(200, 132),
                    90.0f)));

        var originalLayout = button.Layout;

        document.Execute(
            new ReparentEditorUiElementCommand(
                document,
                button.Id,
                expander.Id));

        Assert.Same(expander, button.Parent);

        AssertVector2Near(
            new Vector2(56, -98),
            button.Layout.Offset);

        Assert.Equal(
            -90.0f,
            button.Layout.RotationDegrees);

        document.Undo();

        Assert.Same(source, button.Parent);
        Assert.Equal(originalLayout, button.Layout);

        document.Redo();

        Assert.Same(expander, button.Parent);

        AssertVector2Near(
            new Vector2(56, -98),
            button.Layout.Offset);
    }

    [Fact]
    public void Reparent_ScrollViewRejectsSecondChild()
    {
        var document = new EditorUiDocument();

        var source = document.AddElement(
            EditorUiElementType.Panel,
            document.Root.Id);

        var scrollView = document.AddElement(
            EditorUiElementType.ScrollView,
            document.Root.Id);

        document.AddElement(
            EditorUiElementType.Label,
            scrollView.Id);

        var button = document.AddElement(
            EditorUiElementType.Button,
            source.Id);

        Assert.Throws<InvalidOperationException>(
            () => new ReparentEditorUiElementCommand(
                document,
                button.Id,
                scrollView.Id));
    }

    [Fact]
    public void AddElement_PanelCanContainMultipleChildren()
    {
        var document = new EditorUiDocument();

        var panel = document.AddElement(
            EditorUiElementType.Panel,
            document.Root.Id);

        document.AddElement(
            EditorUiElementType.Button,
            panel.Id);

        document.AddElement(
            EditorUiElementType.Label,
            panel.Id);

        Assert.Equal(2, panel.Children.Count);
    }

    [Fact]
    public void ActionProperty_ExecuteUndoRedo_RestoresAction()
    {
        var document =
            new EditorUiDocument();

        var button =
            document.AddElement(
                EditorUiElementType.Button,
                document.Root.Id,
                "PlayButton");

        document.MarkSaved();

        var inspector =
            new EditorUiInspector(document);

        var actionProperty =
            inspector.GetProperties(button)
                .Single(property =>
                    property.Name == "Action");

        document.Execute(
            new SetEditorPropertyCommand(
                actionProperty,
                "StartGameRequested"));

        Assert.Equal(
            "StartGameRequested",
            button.Action);

        Assert.True(document.IsDirty);

        document.Undo();

        Assert.Null(button.Action);
        Assert.False(document.IsDirty);

        document.Redo();

        Assert.Equal(
            "StartGameRequested",
            button.Action);

        Assert.True(document.IsDirty);
    }

    [Fact]
    public void SetLayoutCommand_ExecuteUndoRedo_RestoresLayout()
    {
        var document = new EditorUiDocument();

        var element = document.AddElement(
            EditorUiElementType.Panel,
            document.Root.Id);

        document.MarkSaved();

        var oldLayout = element.Layout;

        var newLayout = oldLayout with
        {
            Anchor = new Vector2(0.5f, 0.5f),
            Offset = new Vector2(32.0f, 48.0f),
            Size = new Vector2(320.0f, 120.0f)
        };

        document.Execute(
            new SetEditorUiLayoutCommand(
                document,
                element.Id,
                oldLayout,
                newLayout));

        Assert.Equal(newLayout, element.Layout);
        Assert.True(document.IsDirty);

        document.Undo();

        Assert.Equal(oldLayout, element.Layout);
        Assert.False(document.IsDirty);

        document.Redo();

        Assert.Equal(newLayout, element.Layout);
        Assert.True(document.IsDirty);
    }

    [Fact]
    public void ImplementsSharedDocumentContract()
    {
        var document = new EditorUiDocument("HUD");

        IEditorDocument metadata = document;

        Assert.Equal(
            "vibeengine.ui",
            metadata.DocumentTypeId);

        Assert.Equal(
            "HUD",
            metadata.DisplayName);

        Assert.Null(metadata.FilePath);
        Assert.False(metadata.IsDirty);
    }

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

    private static void AssertVector2Near(
    Vector2 expected,
    Vector2 actual,
    float tolerance = 0.001f)
    {
        Assert.InRange(
            MathF.Abs(expected.X - actual.X),
            0.0f,
            tolerance);

        Assert.InRange(
            MathF.Abs(expected.Y - actual.Y),
            0.0f,
            tolerance);
    }
}
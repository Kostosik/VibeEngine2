using Engine.Core.Math;
using Engine.Editor.Commands;
using Engine.Editor.UI.Authoring;

namespace Engine.Tests.Editor.Authoring;

public sealed class EditorUiCanvasHitTesterTests
{
    [Fact]
    public void HitTest_ReturnsDeepestElement()
    {
        var document = new EditorUiDocument();

        var panel = document.AddElement(
            EditorUiElementType.Panel,
            document.Root.Id);

        var button = document.AddElement(
            EditorUiElementType.Button,
            panel.Id);

        SetLayout(
            document,
            panel,
            new EditorUiLayout(
                Vector2.Zero,
                new Vector2(100, 100),
                new Vector2(300, 200)));

        SetLayout(
            document,
            button,
            new EditorUiLayout(
                Vector2.Zero,
                new Vector2(20, 30),
                new Vector2(80, 40)));

        var hit = EditorUiCanvasHitTester.HitTest(
            document,
            new Vector2(150, 150));

        Assert.Same(button, hit);
    }

    [Fact]
    public void HitTest_ReturnsElementWithHighestZIndex()
    {
        var document = new EditorUiDocument();

        var first = document.AddElement(
            EditorUiElementType.Button,
            document.Root.Id);

        var second = document.AddElement(
            EditorUiElementType.Button,
            document.Root.Id);

        SetLayout(
            document,
            first,
            new EditorUiLayout(
                100, 100, 200, 100));

        SetLayout(
            document,
            second,
            new EditorUiLayout(
                100, 100, 200, 100));

        SetProperty(document, first, "ZIndex", 0);
        SetProperty(document, second, "ZIndex", 10);

        var hit = EditorUiCanvasHitTester.HitTest(
            document,
            new Vector2(150, 150));

        Assert.Same(second, hit);
    }

    [Fact]
    public void HitTest_IgnoresInvisibleParent()
    {
        var document = new EditorUiDocument();

        var panel = document.AddElement(
            EditorUiElementType.Panel,
            document.Root.Id);

        var button = document.AddElement(
            EditorUiElementType.Button,
            panel.Id);

        SetLayout(
            document,
            panel,
            new EditorUiLayout(
                100, 100, 300, 200));

        SetLayout(
            document,
            button,
            new EditorUiLayout(
                20, 30, 80, 40));

        SetProperty(document, panel, "Visible", false);

        var hit = EditorUiCanvasHitTester.HitTest(
            document,
            new Vector2(150, 150));

        Assert.Same(document.Root, hit);
    }

    [Fact]
    public void HitTest_AccountsForElementRotation()
    {
        var document = new EditorUiDocument();

        var button = document.AddElement(
            EditorUiElementType.Button,
            document.Root.Id);

        SetLayout(
            document,
            button,
            new EditorUiLayout(
                Vector2.Zero,
                new Vector2(50, 50),
                new Vector2(100, 40),
                90));

        var hit = EditorUiCanvasHitTester.HitTest(
            document,
            new Vector2(100, 45));

        Assert.Same(button, hit);
    }

    [Fact]
    public void HitTest_UsesProvidedLayoutResolver()
    {
        var document = new EditorUiDocument();

        var button = document.AddElement(
            EditorUiElementType.Button,
            document.Root.Id);

        var temporaryLayout = new EditorUiLayout(
            Vector2.Zero,
            new Vector2(500, 300),
            new Vector2(100, 100));

        var hit = EditorUiCanvasHitTester.HitTest(
            document,
            new Vector2(550, 350),
            element => ReferenceEquals(element, button)
                ? temporaryLayout
                : element.Layout);

        Assert.Same(button, hit);
    }

    private static void SetLayout(
        EditorUiDocument document,
        EditorUiElement element,
        EditorUiLayout layout)
    {
        document.Execute(
            new SetEditorUiLayoutCommand(
                document,
                element.Id,
                element.Layout,
                layout));
    }

    private static void SetProperty(
        EditorUiDocument document,
        EditorUiElement element,
        string propertyName,
        object value)
    {
        var property = new EditorUiInspector(document)
            .GetProperties(element)
            .Single(p => p.Name == propertyName);

        document.Execute(
            new SetEditorPropertyCommand(
                property,
                value));
    }
}
using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.Editor;
using Engine.Editor.Documents;
using Engine.Editor.UI.Workspace;
using Engine.Graphics2D.Sprites;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class SpriteTextureInspectorTests
{
    [Fact]
    public void UseSelectedTexture_AssignsPathAndSupportsUndoRedo()
    {
        using var ecsWorld = new Engine.ECS.World();

        var world = new World(
            new ChunkSize(16, 16),
            ecsWorld);

        var entity = ecsWorld.CreateEntity();

        ecsWorld.Add(entity, new WorldTransform2D());
        ecsWorld.Add(entity, new SpriteComponent());

        var editor = new EditorContext();
        var document = editor.OpenDocument(world);

        try
        {
            document.EntitySelection.Set(entity);

            var panel = new InspectorPanelView(
                editor,
                getSelectedTexturePath:
                    () => "Textures/player.png");

            panel.Refresh();

            FindButton(panel, "Use selected").Click();

            Assert.Equal(
                "Textures/player.png",
                ecsWorld.Get<SpriteComponent>(entity).TexturePath);

            document.Undo();

            Assert.Equal(
                string.Empty,
                ecsWorld.Get<SpriteComponent>(entity).TexturePath);

            document.Redo();

            Assert.Equal(
                "Textures/player.png",
                ecsWorld.Get<SpriteComponent>(entity).TexturePath);

            FindButton(panel, "Clear").Click();

            Assert.Equal(
                string.Empty,
                ecsWorld.Get<SpriteComponent>(entity).TexturePath);

            document.Undo();

            Assert.Equal(
                "Textures/player.png",
                ecsWorld.Get<SpriteComponent>(entity).TexturePath);
        }
        finally
        {
            editor.Session.Discard(document);
        }
    }

    private static UiButton FindButton(
        UiWidget root,
        string text)
    {
        return Descendants(root)
            .OfType<UiButton>()
            .Single(button => button.Text == text);
    }

    private static IEnumerable<UiWidget> Descendants(
        UiWidget widget)
    {
        yield return widget;

        if (widget is not UiContainer container)
        {
            yield break;
        }

        foreach (var child in container.Children)
        {
            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}
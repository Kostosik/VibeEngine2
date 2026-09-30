using Engine.Core.Math;
using Engine.ECS.Entities;
using Engine.Editor;
using Engine.Editor.UI.Workspace;
using Engine.Worlds;
using Engine.Worlds.Spatial;
using System.Reflection;

namespace Engine.Tests.Editor;

public sealed class ViewportPanelViewTests
{
    [Fact]
    public void EntityHitTest_ScalesWithWorldMarkerAtHighZoom()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var entity =
            world.SpatialEntities.CreateEntity(
                new WorldPosition(0, 0));

        var editor =
            new EditorContext();

        var document =
            editor.OpenDocument(
                world);

        document.Viewport.State.SetZoom(
            4.0f);

        var viewport =
            new ViewportPanelView(
                editor);

        var findEntityAt =
            typeof(ViewportPanelView)
                .GetMethod(
                    "FindEntityAt",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "FindEntityAt method was not found.");

        var result =
            findEntityAt.Invoke(
                viewport,
                new object[]
                {
            document,
            new Vector2(4.9f, 0.0f)
                });

        Assert.NotNull(result);

        Assert.Equal(
            entity,
            Assert.IsType<EntityId>(
                result));
    }
}
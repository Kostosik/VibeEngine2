
using Engine.Core.Math;
using Engine.Editor.Documents;
using Engine.Editor.Inspection;
using Engine.Editor.UI.Inspection;
using Engine.Physics.Components;
using Engine.Physics.Shapes;
using Engine.UI.Layout;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class PhysicsShapeInspectorPropertyEditorTests
{
    [Fact]
    public void CreateDefault_SelectsEditorForPhysicsShape2D()
    {
        var registry =
            InspectorPropertyEditorRegistry.CreateDefault();

        Assert.True(
            registry.TryGetEditor(
                typeof(PhysicsShape2D),
                out var editor));

        Assert.NotNull(editor);

        Assert.True(
            editor.CanEdit(typeof(PhysicsShape2D)));

        Assert.Equal(
            300,
            editor.Priority);
    }

    [Fact]
    public void Create_ProvidesShapeSelectionAndControls()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var entity =
            ecsWorld.CreateEntity();

        ecsWorld.Add(
            entity,
            new Collider2D(
                new AabbShape2D(
                    new FixedVector2(
                        Fixed32.One,
                        Fixed32.One))));

        using var document =
            new EditorDocument(world);

        var shapeProperty =
            Assert.Single(
                document.Inspector.GetProperties(
                    entity,
                    typeof(Collider2D))
                .Where(
                    property =>
                        property.Name ==
                        nameof(Collider2D.Shape)));

        var registry =
            InspectorPropertyEditorRegistry.CreateDefault();

        var widget =
            registry.Create(
                document,
                shapeProperty);

        var panel =
            Assert.IsType<UiStackPanel>(widget);

        Assert.Equal(
            7,
            panel.Children.Count);
    }
}

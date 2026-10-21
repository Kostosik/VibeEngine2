using Engine.Core.Math;
using Engine.Editor.Inspection;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class EcsComponentPropertyProviderTests
{
    [Fact]
    public void GetProperties_PrivateSetterIsReadOnly()
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
            new TestComponent());

        var target =
            new EcsComponentEditorTarget(
                world,
                entity,
                typeof(TestComponent));

        var provider =
            new EcsComponentPropertyProvider();

        var properties =
            provider.GetProperties(target);

        var editableProperty =
            Assert.Single(
                properties.Where(
                    property =>
                        property.Name ==
                        nameof(TestComponent.EditableValue)));

        var privateSetterProperty =
            Assert.Single(
                properties.Where(
                    property =>
                        property.Name ==
                        nameof(TestComponent.ReadOnlyValue)));

        Assert.False(editableProperty.IsReadOnly);
        Assert.True(privateSetterProperty.IsReadOnly);
    }

    private struct TestComponent
    {
        public int EditableValue { get; set; }

        public int ReadOnlyValue { get; private set; }
    }
}
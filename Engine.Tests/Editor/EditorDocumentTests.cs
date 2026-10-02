using Engine.ECS.Entities;
using Engine.Editor.Commands;
using Engine.Editor.Documents;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class EditorDocumentTests
{
    [Fact]
    public void Reference_AfterWorldSnapshotRestore_IsNotAliveWhenEntityWasRemoved()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var snapshot =
            ecsWorld.CreateSnapshot();

        var entity =
            ecsWorld.CreateEntity();

        var document =
            new EditorDocument(
                world);

        var reference =
            document.GetEntityReference(
                entity);

        Assert.True(
            reference.IsAlive);

        ecsWorld.RestoreSnapshot(
            snapshot);

        Assert.False(
            reference.IsAlive);
    }

    [Fact]
    public void EntityDestroyed_GetEntityReferenceThrows()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var entity =
            ecsWorld.CreateEntity();

        var document =
            new EditorDocument(
                world);

        Assert.True(
            ecsWorld.DestroyEntity(
                entity));

        Assert.Throws<InvalidOperationException>(
            () =>
                document.GetEntityReference(
                    entity));
    }

    [Fact]
    public void EntityDestroyed_InspectorRejectsInvalidReference()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var entity =
            ecsWorld.CreateEntity();

        var document =
            new EditorDocument(
                world);

        var reference =
            document.GetEntityReference(
                entity);

        Assert.True(
            ecsWorld.DestroyEntity(
                entity));

        Assert.False(
            reference.IsAlive);

        Assert.Throws<InvalidOperationException>(
            () =>
                document.Inspector.GetProperties(
                    reference,
                    typeof(TestComponent)));
    }

    [Fact]
    public void EntityDestroyed_RemovesSelectionAndInvalidatesReference()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var entity =
            ecsWorld.CreateEntity();

        var document =
            new EditorDocument(
                world);

        var reference =
            document.GetEntityReference(
                entity);

        document.EntitySelection.Set(
            entity);

        Assert.True(
            reference.IsAlive);

        Assert.True(
            document.EntitySelection.Contains(
                entity));

        Assert.True(
            ecsWorld.DestroyEntity(
                entity));

        Assert.False(
            reference.IsAlive);

        Assert.False(
            document.EntitySelection.Contains(
                entity));

        Assert.Empty(
            document.EntitySelection.Items);
    }

    [Fact]
    public void Reference_CannotBeUsedWithAnotherWorld()
    {
        using var firstEcsWorld =
            new Engine.ECS.World();

        using var secondEcsWorld =
            new Engine.ECS.World();

        var firstWorld =
            new World(
                new ChunkSize(16, 16),
                firstEcsWorld);

        var secondWorld =
            new World(
                new ChunkSize(16, 16),
                secondEcsWorld);

        var firstEntity =
            firstEcsWorld.CreateEntity();

        var secondEntity =
            secondEcsWorld.CreateEntity();

        Assert.Equal(
            firstEntity.Index,
            secondEntity.Index);

        var firstDocument =
            new EditorDocument(
                firstWorld);

        var secondDocument =
            new EditorDocument(
                secondWorld);

        var reference =
            firstDocument.GetEntityReference(
                firstEntity);

        Assert.Throws<InvalidOperationException>(
            () =>
                secondDocument.Inspector.GetProperties(
                    reference,
                    typeof(TestComponent)));

        Assert.Throws<InvalidOperationException>(
            () =>
                new SetWorldPositionCommand(
                    secondWorld,
                    reference,
                    new WorldPosition(0, 0),
                    new WorldPosition(1, 1)));
    }

    private struct TestComponent
    {
        public int Value { get; set; }
    }
}
using Engine.ECS.Entities;
using Engine.Editor.Commands;
using Engine.Editor.Documents;
using Engine.Editor.Inspection;
using Engine.Editor.Entities;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class EditorCommandTests
{
    [Fact]
    public void CreateEntity_ExecuteUndoRedo_RestoresEntityLifecycle()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var document =
            new EditorDocument(
                world);

        var command =
            new CreateEditorEntityCommand(
                document);

        command.Execute();

        var firstEntity =
            command.Entity;

        Assert.True(
            firstEntity.IsValid);

        Assert.True(
            ecsWorld.Exists(
                firstEntity));

        Assert.True(
            command.Reference!.IsAlive);

        command.Undo();

        Assert.False(
            ecsWorld.Exists(
                firstEntity));

        Assert.False(
            command.Reference.IsAlive);

        command.Execute();

        Assert.True(
            command.Reference.IsAlive);

        Assert.True(
            ecsWorld.Exists(
                command.Entity));

        Assert.True(
            world.SpatialEntities.Contains(
                command.Entity));
    }

    [Fact]
    public void DeleteSpatialEntity_ExecuteUndoRedo_RestoresPositionAndComponents()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var entity =
            world.SpatialEntities.CreateEntity(
                new WorldPosition(7, 9));

        ecsWorld.Add(
            entity,
            new TestComponent
            {
                Value = 42
            });

        var document =
            new EditorDocument(
                world);

        var reference =
            document.GetEntityReference(
                entity);

        var command =
            new DeleteEditorEntityCommand(
                document,
                entity);

        command.Execute();

        Assert.False(
            ecsWorld.Exists(
                entity));

        Assert.False(
            reference.IsAlive);

        command.Undo();

        var restoredEntity =
            reference.Entity;

        Assert.True(
            reference.IsAlive);

        Assert.True(
            ecsWorld.Exists(
                restoredEntity));

        Assert.Equal(
            new WorldPosition(7, 9),
            world.SpatialEntities.GetPosition(
                restoredEntity));

        var restoredComponent =
            ecsWorld.Get<TestComponent>(
                restoredEntity);

        Assert.Equal(
            42,
            restoredComponent.Value);

        command.Execute();

        Assert.False(
            ecsWorld.Exists(
                restoredEntity));

        Assert.False(
            reference.IsAlive);
    }

    [Fact]
    public void DeleteNonSpatialEntity_ExecuteUndo_RestoresComponents()
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
            new TestComponent
            {
                Value = 99
            });

        var document =
            new EditorDocument(
                world);

        var reference =
            document.GetEntityReference(
                entity);

        var command =
            new DeleteEditorEntityCommand(
                document,
                entity);

        command.Execute();

        Assert.False(
            ecsWorld.Exists(
                entity));

        command.Undo();

        Assert.True(
            reference.IsAlive);

        var restoredComponent =
            ecsWorld.Get<TestComponent>(
                reference.Entity);

        Assert.Equal(
            99,
            restoredComponent.Value);
    }

    [Fact]
    public void AddComponent_ExecuteUndoRedo_ChangesComponentPresence()
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

        var document =
            new EditorDocument(
                world);

        var reference =
            document.GetEntityReference(
                entity);

        var command =
            new AddEditorComponentCommand(
                world,
                reference,
                typeof(TestComponent));

        Assert.False(
            ecsWorld.Has<TestComponent>(
                entity));

        command.Execute();

        Assert.True(
            ecsWorld.Has<TestComponent>(
                entity));

        Assert.Equal(
            0,
            ecsWorld.Get<TestComponent>(
                entity).Value);

        command.Undo();

        Assert.False(
            ecsWorld.Has<TestComponent>(
                entity));

        command.Execute();

        Assert.True(
            ecsWorld.Has<TestComponent>(
                entity));
    }

    [Fact]
    public void RemoveComponent_ExecuteUndoRedo_RestoresOriginalValue()
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

        ecsWorld.Add(
            entity,
            new TestComponent
            {
                Value = 123
            });

        var document =
            new EditorDocument(
                world);

        var reference =
            document.GetEntityReference(
                entity);

        var command =
            new RemoveEditorComponentCommand(
                world,
                reference,
                typeof(TestComponent));

        command.Execute();

        Assert.False(
            ecsWorld.Has<TestComponent>(
                entity));

        command.Undo();

        Assert.True(
            ecsWorld.Has<TestComponent>(
                entity));

        Assert.Equal(
            123,
            ecsWorld.Get<TestComponent>(
                entity).Value);

        command.Execute();

        Assert.False(
            ecsWorld.Has<TestComponent>(
                entity));
    }

    [Fact]
    public void SetEditorProperty_ExecuteUndoRedo_RestoresOriginalValue()
    {
        var target =
            new TestTarget
            {
                Value = 10
            };

        var property =
            new EditorProperty(
                nameof(TestTarget.Value),
                typeof(int),
                () => target.Value,
                value =>
                    target.Value =
                        (int)value!);

        var command =
            new SetEditorPropertyCommand(
                property,
                25);

        command.Execute();

        Assert.Equal(
            25,
            target.Value);

        command.Undo();

        Assert.Equal(
            10,
            target.Value);

        command.Execute();

        Assert.Equal(
            25,
            target.Value);
    }

    [Fact]
    public void SetWorldPosition_ExecuteUndoRedo_RestoresPosition()
    {
        using var ecsWorld =
            new Engine.ECS.World();

        var world =
            new World(
                new ChunkSize(16, 16),
                ecsWorld);

        var entity =
            world.SpatialEntities.CreateEntity(
                new WorldPosition(2, 3));

        var document =
            new EditorDocument(
                world);

        var reference =
            document.GetEntityReference(
                entity);

        var command =
            new SetWorldPositionCommand(
                world,
                reference,
                new WorldPosition(2, 3),
                new WorldPosition(10, 12));

        command.Execute();

        Assert.Equal(
            new WorldPosition(10, 12),
            world.SpatialEntities.GetPosition(
                entity));

        command.Undo();

        Assert.Equal(
            new WorldPosition(2, 3),
            world.SpatialEntities.GetPosition(
                entity));

        command.Execute();

        Assert.Equal(
            new WorldPosition(10, 12),
            world.SpatialEntities.GetPosition(
                entity));
    }

    private struct TestComponent
    {
        public int Value { get; set; }
    }

    private sealed class TestTarget
    {
        public int Value { get; set; }
    }
}
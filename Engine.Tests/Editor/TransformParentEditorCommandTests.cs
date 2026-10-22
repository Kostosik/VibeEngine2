using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Editor.Commands;
using Engine.Editor.Documents;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class TransformParentEditorCommandTests
{
    [Fact]
    public void SetParent_PreservesWorldTransform_AndSupportsUndoRedo()
    {
        using var ecsWorld = new Engine.ECS.World();

        using var world = new World(
            new ChunkSize(16, 16),
            ecsWorld);

        using var document = new EditorDocument(world);

        var parent = ecsWorld.CreateEntity();
        var child = ecsWorld.CreateEntity();

        ecsWorld.Add(
            parent,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromInt(10),
                    Fixed32.FromInt(20))));

        ecsWorld.Add(
            child,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromInt(3),
                    Fixed32.FromInt(4))));

        var command = new SetTransformParent2DCommand(
            world,
            document.GetEntityReference(child),
            parent);

        document.Execute(command);

        Assert.True(ecsWorld.Has<TransformParent2D>(child));
        Assert.True(ecsWorld.Has<LocalTransform2D>(child));

        Assert.Equal(
            parent,
            ecsWorld.Get<TransformParent2D>(child).Parent);

        Assert.Equal(
            Fixed32.FromInt(3),
            ecsWorld.Get<WorldTransform2D>(child).Position.X);

        Assert.Equal(
            Fixed32.FromInt(4),
            ecsWorld.Get<WorldTransform2D>(child).Position.Y);

        document.Undo();

        Assert.False(ecsWorld.Has<TransformParent2D>(child));
        Assert.False(ecsWorld.Has<LocalTransform2D>(child));

        document.Redo();

        Assert.True(ecsWorld.Has<TransformParent2D>(child));
        Assert.Equal(
            parent,
            ecsWorld.Get<TransformParent2D>(child).Parent);

        Assert.Equal(
            Fixed32.FromInt(3),
            ecsWorld.Get<WorldTransform2D>(child).Position.X);

        Assert.Equal(
            Fixed32.FromInt(4),
            ecsWorld.Get<WorldTransform2D>(child).Position.Y);
    }

    [Fact]
    public void ClearParent_PreservesWorldTransform()
    {
        using var ecsWorld = new Engine.ECS.World();

        using var world = new World(
            new ChunkSize(16, 16),
            ecsWorld);

        using var document = new EditorDocument(world);

        var parent = ecsWorld.CreateEntity();
        var child = ecsWorld.CreateEntity();

        ecsWorld.Add(
            parent,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromInt(10),
                    Fixed32.FromInt(20))));

        ecsWorld.Add(
            child,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromInt(3),
                    Fixed32.FromInt(4))));

        document.Execute(
            new SetTransformParent2DCommand(
                world,
                document.GetEntityReference(child),
                parent));

        document.Execute(
            new SetTransformParent2DCommand(
                world,
                document.GetEntityReference(child),
                null));

        Assert.False(ecsWorld.Has<TransformParent2D>(child));
        Assert.True(ecsWorld.Has<LocalTransform2D>(child));

        Assert.Equal(
            Fixed32.FromInt(3),
            ecsWorld.Get<WorldTransform2D>(child).Position.X);

        Assert.Equal(
            Fixed32.FromInt(4),
            ecsWorld.Get<WorldTransform2D>(child).Position.Y);

        document.Undo();

        Assert.True(ecsWorld.Has<TransformParent2D>(child));
        Assert.Equal(
            parent,
            ecsWorld.Get<TransformParent2D>(child).Parent);
    }

    [Fact]
    public void SetParent_RejectsHierarchyCycle()
    {
        using var ecsWorld = new Engine.ECS.World();

        using var world = new World(
            new ChunkSize(16, 16),
            ecsWorld);

        using var document = new EditorDocument(world);

        var parent = ecsWorld.CreateEntity();
        var child = ecsWorld.CreateEntity();

        ecsWorld.Add(parent, new WorldTransform2D());
        ecsWorld.Add(child, new WorldTransform2D());

        document.Execute(
            new SetTransformParent2DCommand(
                world,
                document.GetEntityReference(child),
                parent));

        var exception = Assert.Throws<InvalidOperationException>(
            () => document.Execute(
                new SetTransformParent2DCommand(
                    world,
                    document.GetEntityReference(parent),
                    child)));

        Assert.Contains("cycle", exception.Message);

        Assert.False(ecsWorld.Has<TransformParent2D>(parent));
    }
}
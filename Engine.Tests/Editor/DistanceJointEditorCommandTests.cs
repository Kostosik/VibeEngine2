using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Editor.Commands;
using Engine.Editor.Documents;
using Engine.Physics.Components;
using Engine.Physics.Joints;
using Engine.Worlds;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Editor;

public sealed class DistanceJointEditorCommandTests
{
    [Fact]
    public void AddDistanceJoint_ExecutesAndSupportsUndoRedo()
    {
        using var ecsWorld = new Engine.ECS.World();

        using var world = new World(
            new ChunkSize(16, 16),
            ecsWorld);

        using var document = new EditorDocument(world);

        var first = ecsWorld.CreateEntity();
        var second = ecsWorld.CreateEntity();

        AddPhysicsEntity(ecsWorld, first);
        AddPhysicsEntity(ecsWorld, second);

        document.Execute(
            new AddDistanceJoint2DCommand(
                world,
                document.GetEntityReference(first),
                second,
                Fixed32.FromInt(5)));

        Assert.True(
            ecsWorld.Has<DistanceJoint2D>(first));

        var joint = ecsWorld.Get<DistanceJoint2D>(first);

        Assert.Equal(first, joint.First);
        Assert.Equal(second, joint.Second);
        Assert.Equal(Fixed32.FromInt(5), joint.Length);
        Assert.True(joint.Enabled);

        document.Undo();

        Assert.False(
            ecsWorld.Has<DistanceJoint2D>(first));

        document.Redo();

        Assert.True(
            ecsWorld.Has<DistanceJoint2D>(first));

        joint = ecsWorld.Get<DistanceJoint2D>(first);

        Assert.Equal(second, joint.Second);
        Assert.Equal(Fixed32.FromInt(5), joint.Length);
    }

    [Fact]
    public void AddDistanceJoint_RejectsEndpointWithoutPhysicsBody()
    {
        using var ecsWorld = new Engine.ECS.World();

        using var world = new World(
            new ChunkSize(16, 16),
            ecsWorld);

        using var document = new EditorDocument(world);

        var first = ecsWorld.CreateEntity();
        var second = ecsWorld.CreateEntity();

        AddPhysicsEntity(ecsWorld, first);

        ecsWorld.Add(
            second,
            new WorldTransform2D(FixedVector2.Zero));

        Assert.Throws<InvalidOperationException>(
            () => document.Execute(
                new AddDistanceJoint2DCommand(
                    world,
                    document.GetEntityReference(first),
                    second,
                    Fixed32.One)));

        Assert.False(
            ecsWorld.Has<DistanceJoint2D>(first));

        Assert.Equal(0, document.CommandHistory.UndoCount);
    }

    private static void AddPhysicsEntity(
        Engine.ECS.World world,
        EntityId entity)
    {
        world.Add(
            entity,
            new WorldTransform2D(FixedVector2.Zero));

        world.Add(
            entity,
            PhysicsBody2D.Dynamic(Fixed32.One));
    }
}
using Engine.Core.Math;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Editor.Entities;
using Engine.Editor.Inspection;
using Engine.Physics.Components;
using Engine.Physics.Joints;
using Engine.Worlds;

namespace Engine.Editor.Commands;

public sealed class AddDistanceJoint2DCommand : IEditorCommand
{
    private readonly World _world;
    private readonly EditorEntityReference _firstReference;
    private readonly EntityId _second;
    private readonly Fixed32 _length;

    private bool _executed;

    public AddDistanceJoint2DCommand(
        World world,
        EditorEntityReference firstReference,
        EntityId second,
        Fixed32 length)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(firstReference);

        firstReference.EnsureWorld(world);

        if (length <= Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length),
                "Joint length must be greater than zero.");
        }

        _world = world;
        _firstReference = firstReference;
        _second = second;
        _length = length;
    }

    public void Execute()
    {
        if (!_firstReference.IsAlive)
        {
            throw new InvalidOperationException(
                "The first joint entity is no longer alive.");
        }

        var first = _firstReference.Entity;

        if (first == _second)
        {
            throw new InvalidOperationException(
                "A distance joint requires two different entities.");
        }

        ValidateEndpoint(first, "first");
        ValidateEndpoint(_second, "second");

        if (_world.EcsWorld.Has<DistanceJoint2D>(first))
        {
            throw new InvalidOperationException(
                "The selected entity already has a DistanceJoint2D.");
        }

        var joint = new DistanceJoint2D(
            first,
            _second,
            _length);

        EcsComponentAccessor.Add(
            _world,
            first,
            typeof(DistanceJoint2D),
            joint);

        _executed = true;
    }

    public void Undo()
    {
        if (!_executed)
        {
            throw new InvalidOperationException(
                "The command has not been executed.");
        }

        if (!_firstReference.IsAlive ||
            !_world.EcsWorld.Remove<DistanceJoint2D>(
                _firstReference.Entity))
        {
            throw new InvalidOperationException(
                "The distance joint could not be removed.");
        }

        _executed = false;
    }

    private void ValidateEndpoint(
        EntityId entity,
        string endpointName)
    {
        if (!_world.EcsWorld.Exists(entity))
        {
            throw new InvalidOperationException(
                $"The {endpointName} joint entity does not exist.");
        }

        if (!_world.EcsWorld.Has<WorldTransform2D>(entity))
        {
            throw new InvalidOperationException(
                $"The {endpointName} joint entity requires WorldTransform2D.");
        }

        if (!_world.EcsWorld.Has<PhysicsBody2D>(entity))
        {
            throw new InvalidOperationException(
                $"The {endpointName} joint entity requires PhysicsBody2D.");
        }
    }
}
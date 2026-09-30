using Engine.Core.Systems;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Transform;

namespace Engine.ECS.Systems;

public sealed class TransformHierarchySystem :
    IFixedUpdateSystem
{
    private enum VisitState : byte
    {
        Visiting = 1,
        Resolved = 2
    }

    private readonly World _world;

    private readonly Dictionary<
        EntityId,
        VisitState> _states = new();

    public TransformHierarchySystem(
        World world)
    {
        ArgumentNullException.ThrowIfNull(
            world);

        _world = world;
    }

    public void FixedUpdate(
        FixedSystemContext context)
    {
        _states.Clear();

        foreach (var item in
                 _world.Query<LocalTransform2D>())
        {
            Resolve(
                item.Entity);
        }
    }

    private FixedTransform2D Resolve(
        EntityId entity)
    {
        if (_states.TryGetValue(
                entity,
                out var state))
        {
            if (state == VisitState.Resolved)
            {
                return ReadWorldTransform(
                    entity);
            }

            throw new InvalidOperationException(
                $"Transform hierarchy contains a cycle at entity '{entity.Index}'.");
        }

        _states[entity] =
            VisitState.Visiting;

        if (!_world.Has<WorldTransform2D>(
                entity))
        {
            throw new InvalidOperationException(
                $"Entity '{entity.Index}' has LocalTransform2D " +
                "but no Transform2D world component.");
        }

        var local =
            _world
                .Get<LocalTransform2D>(
                    entity)
                .Value;

        FixedTransform2D world;

        if (_world.Has<TransformParent2D>(
                entity))
        {
            var parent =
                _world
                    .Get<TransformParent2D>(
                        entity)
                    .Parent;

            if (!_world.Exists(
                    parent))
            {
                throw new InvalidOperationException(
                    $"Entity '{entity.Index}' references " +
                    $"missing parent '{parent.Index}'.");
            }

            if (!_world.Has<LocalTransform2D>(
                    parent) &&
                _world.Has<TransformParent2D>(
                    parent))
            {
                throw new InvalidOperationException(
                    $"Entity '{parent.Index}' has TransformParent2D " +
                    "but no LocalTransform2D.");
            }

            var parentWorld =
                ResolveParent(
                    parent);

            world =
                FixedTransformOperations2D.Combine(
                    parentWorld,
                    local);
        }
        else
        {
            world = local;
        }

        WriteWorldTransform(
            entity,
            world);

        _states[entity] =
            VisitState.Resolved;

        return world;
    }

    private FixedTransform2D ResolveParent(
        EntityId parent)
    {
        if (_world.Has<LocalTransform2D>(
                parent))
        {
            return Resolve(parent);
        }

        return ReadWorldTransform(
            parent);
    }

    private FixedTransform2D ReadWorldTransform(
        EntityId entity)
    {
        var transform =
            _world.Get<WorldTransform2D>(
                entity);

        return new FixedTransform2D(
            transform.Position)
        {
            Rotation = transform.Rotation,
            Scale = transform.Scale
        };
    }

    private void WriteWorldTransform(
        EntityId entity,
        FixedTransform2D value)
    {
        ref var transform =
            ref _world.Get<WorldTransform2D>(
                entity);

        transform.Position =
            value.Position;

        transform.Rotation =
            value.Rotation;

        transform.Scale =
            value.Scale;
    }
}
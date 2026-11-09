using Engine.Core.Systems;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Transform;

namespace Engine.ECS.Systems;

public sealed class FixedTransformHierarchy3DSystem : IFixedUpdateSystem
{
    private enum VisitState : byte
    {
        Visiting = 1,
        Resolved = 2
    }

    private readonly World _world;
    private readonly Dictionary<EntityId, VisitState> _states = new();

    public FixedTransformHierarchy3DSystem(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        _world = world;
    }

    public void FixedUpdate(FixedSystemContext context)
    {
        _states.Clear();

        foreach (var item in _world.QueryReadOnly<FixedTransformParent3D>())
        {
            if (!_world.Has<FixedLocalTransform3D>(item.Entity))
            {
                throw new InvalidOperationException(
                    $"Entity '{item.Entity.Index}' has FixedTransformParent3D " +
                    "but no FixedLocalTransform3D.");
            }
        }

        foreach (var item in _world.QueryReadOnly<FixedLocalTransform3D>())
        {
            Resolve(item.Entity);
        }
    }

    private FixedTransform3D Resolve(EntityId entity)
    {
        if (_states.TryGetValue(entity, out var state))
        {
            if (state == VisitState.Resolved)
                return ReadWorldTransform(entity);

            throw new InvalidOperationException(
                $"Fixed transform hierarchy contains a cycle at entity '{entity.Index}'.");
        }

        _states[entity] = VisitState.Visiting;

        if (!_world.Has<FixedWorldTransform3D>(entity))
        {
            throw new InvalidOperationException(
                $"Entity '{entity.Index}' has FixedLocalTransform3D " +
                "but no FixedWorldTransform3D.");
        }

        var local = _world
            .GetReadOnly<FixedLocalTransform3D>(entity)
            .Value;

        FixedTransform3D world;

        if (_world.Has<FixedTransformParent3D>(entity))
        {
            var parent = _world
                .GetReadOnly<FixedTransformParent3D>(entity)
                .Parent;

            if (!_world.Exists(parent))
            {
                throw new InvalidOperationException(
                    $"Entity '{entity.Index}' references " +
                    $"missing parent '{parent.Index}'.");
            }

            if (!_world.Has<FixedWorldTransform3D>(parent))
            {
                throw new InvalidOperationException(
                    $"Entity '{parent.Index}' is a parent but has no " +
                    "FixedWorldTransform3D.");
            }

            if (!_world.Has<FixedLocalTransform3D>(parent) &&
                _world.Has<FixedTransformParent3D>(parent))
            {
                throw new InvalidOperationException(
                    $"Entity '{parent.Index}' has FixedTransformParent3D " +
                    "but no FixedLocalTransform3D.");
            }

            var parentWorld = ResolveParent(parent);

            world = FixedTransformOperations3D.Combine(
                parentWorld,
                local);
        }
        else
        {
            world = local;
        }

        _world.Get<FixedWorldTransform3D>(entity).Value = world;

        _states[entity] = VisitState.Resolved;

        return world;
    }

    private FixedTransform3D ResolveParent(EntityId parent)
    {
        if (_world.Has<FixedLocalTransform3D>(parent))
            return Resolve(parent);

        return ReadWorldTransform(parent);
    }

    private FixedTransform3D ReadWorldTransform(EntityId entity)
    {
        return _world
            .GetReadOnly<FixedWorldTransform3D>(entity)
            .Value;
    }
}
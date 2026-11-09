using Engine.Core.Systems;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Transform;

namespace Engine.ECS.Systems;

public sealed class TransformHierarchy3DSystem : IUpdateSystem
{
    private enum VisitState : byte
    {
        Visiting = 1,
        Resolved = 2
    }

    private readonly World _world;

    private readonly Dictionary<EntityId, VisitState> _states = new();

    public TransformHierarchy3DSystem(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        _world = world;
    }

    public void Update(SystemContext context)
    {
        _states.Clear();

        // Каждый объект с родителем обязан иметь локальную трансформацию.
        foreach (var item in _world.QueryReadOnly<TransformParent3D>())
        {
            if (!_world.Has<LocalTransform3D>(item.Entity))
            {
                throw new InvalidOperationException(
                    $"Entity '{item.Entity.Index}' has TransformParent3D " +
                    "but no LocalTransform3D.");
            }
        }

        // Рассчитываем мировые трансформации.
        foreach (var item in _world.QueryReadOnly<LocalTransform3D>())
        {
            Resolve(item.Entity);
        }
    }

    private Transform3D Resolve(EntityId entity)
    {
        if (_states.TryGetValue(entity, out var state))
        {
            if (state == VisitState.Resolved)
            {
                return ReadWorldTransform(entity);
            }

            throw new InvalidOperationException(
                $"Transform hierarchy contains a cycle at entity '{entity.Index}'.");
        }

        _states[entity] = VisitState.Visiting;

        if (!_world.Has<WorldTransform3D>(entity))
        {
            throw new InvalidOperationException(
                $"Entity '{entity.Index}' has LocalTransform3D " +
                "but no WorldTransform3D.");
        }

        var local = _world
            .GetReadOnly<LocalTransform3D>(entity)
            .Value;

        Transform3D world;

        if (_world.Has<TransformParent3D>(entity))
        {
            var parent = _world
                .GetReadOnly<TransformParent3D>(entity)
                .Parent;

            if (!_world.Exists(parent))
            {
                throw new InvalidOperationException(
                    $"Entity '{entity.Index}' references " +
                    $"missing parent '{parent.Index}'.");
            }

            if (!_world.Has<WorldTransform3D>(parent))
            {
                throw new InvalidOperationException(
                    $"Entity '{parent.Index}' is a parent of entity " +
                    $"'{entity.Index}' but has no WorldTransform3D.");
            }

            if (!_world.Has<LocalTransform3D>(parent) &&
                _world.Has<TransformParent3D>(parent))
            {
                throw new InvalidOperationException(
                    $"Entity '{parent.Index}' has TransformParent3D " +
                    "but no LocalTransform3D.");
            }

            var parentWorld = ResolveParent(parent);

            world = TransformOperations3D.Combine(
                parentWorld,
                local);
        }
        else
        {
            world = local;
        }

        WriteWorldTransform(entity, world);

        _states[entity] = VisitState.Resolved;

        return world;
    }

    private Transform3D ResolveParent(EntityId parent)
    {
        if (_world.Has<LocalTransform3D>(parent))
        {
            return Resolve(parent);
        }

        return ReadWorldTransform(parent);
    }

    private Transform3D ReadWorldTransform(EntityId entity)
    {
        return _world
            .GetReadOnly<WorldTransform3D>(entity)
            .Value;
    }

    private void WriteWorldTransform(
        EntityId entity,
        Transform3D value)
    {
        _world.Get<WorldTransform3D>(entity).Value = value;
    }
}
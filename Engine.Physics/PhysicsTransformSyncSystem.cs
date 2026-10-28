using Engine.Core.Systems;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.Physics.Components;

namespace Engine.Physics;

public sealed class PhysicsTransformSyncSystem :
    IFixedUpdateSystem
{
    private readonly World _world;

    public PhysicsTransformSyncSystem(
        World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        _world = world;
    }

    public void FixedUpdate(
        FixedSystemContext context)
    {
        foreach (var item in
                 _world.Query<PhysicsBody2D>())
        {
            var bodyType =
                item.Component.BodyType;

            if (bodyType != PhysicsBodyType.Dynamic &&
                bodyType != PhysicsBodyType.Kinematic)
            {
                continue;
            }

            var entity = item.Entity;

            // Иерархические трансформы здесь не синхронизируем:
            // динамические и кинематические тела должны быть корневыми.
            if (_world.Has<TransformParent2D>(entity))
            {
                continue;
            }

            if (!_world.Has<LocalTransform2D>(entity) ||
                !_world.Has<WorldTransform2D>(entity))
            {
                continue;
            }

            var worldTransform =
                _world.Get<WorldTransform2D>(entity);

            ref var localTransform =
                ref _world.Get<LocalTransform2D>(entity);

            localTransform.Position =
                worldTransform.Position;

            localTransform.Rotation =
                worldTransform.Rotation;

            localTransform.Scale =
                worldTransform.Scale;
        }
    }
}
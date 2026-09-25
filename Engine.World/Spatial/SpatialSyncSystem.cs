using Engine.Core.Math;
using Engine.Core.Systems;
using Engine.ECS;
using Engine.ECS.Components;

namespace Engine.Worlds.Spatial;

public sealed class SpatialSyncSystem :
    IFixedUpdateSystem
{
    private readonly World _world;
    private readonly Engine.ECS.World _ecsWorld;

    public SpatialSyncSystem(
        World world,
        Engine.ECS.World ecsWorld)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(ecsWorld);

        _world = world;
        _ecsWorld = ecsWorld;
    }

    public void FixedUpdate(
        FixedSystemContext context)
    {
        foreach (var item
                 in _ecsWorld.Query<Transform2D>())
        {
            ref var transform =
                ref item.Component;

            var position =
                ToWorldPosition(
                    transform.Position);

            _world.SpatialEntities.SetPosition(
                item.Entity,
                position);
        }
    }

    private static WorldPosition ToWorldPosition(
        FixedVector2 position)
    {
        return new WorldPosition(
            position.X.FloorToInt(),
            position.Y.FloorToInt());
    }
}
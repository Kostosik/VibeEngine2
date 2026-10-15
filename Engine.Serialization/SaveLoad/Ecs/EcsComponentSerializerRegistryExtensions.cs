using Engine.Serialization.Types;

namespace Engine.Serialization.SaveLoad.Ecs;

public static class EcsComponentSerializerRegistryExtensions
{
    public static void RegisterEngineComponents(
        this EcsComponentSerializerRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register(
            "engine.world.transform2d",
            new WorldTransform2DSerializer());

        registry.Register(
            "engine.physics.body2d",
            new PhysicsBody2DSerializer());

        registry.Register(
            "engine.physics.collider2d",
            new Collider2DSerializer());

        registry.Register(
            "engine.physics.joint.distance2d",
            new DistanceJoint2DSerializer());

        registry.Register(
            "engine.world.position",
            new WorldPositionComponentSerializer());
    }
}
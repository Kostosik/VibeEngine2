using Engine.Core.Math;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.ECS.Components;
using Engine.Physics;
using Engine.Physics.Components;
using Engine.Runtime;
using Engine.Transform;
using Engine.Worlds.Spatial;

namespace Engine.Tests.Runtime;

public sealed class TransformRuntimeIntegrationTests
{
    [Fact]
    public void FixedUpdate_ResolvesLocalTransformAndSynchronizesSpatialPosition()
    {
        using var runtime = new EngineRuntime(
            new EngineRuntimeOptions
            {
                ToolingEnabled = false
            });

        runtime.Initialize();

        var entity = runtime.World.SpatialEntities.CreateEntity(
            new WorldPosition(0, 0));

        runtime.EcsWorld.Add(
            entity,
            new WorldTransform2D(FixedVector2.Zero));

        runtime.EcsWorld.Add(
            entity,
            new LocalTransform2D(
                new FixedTransform2D(
                    new FixedVector2(
                        Fixed32.FromInt(5),
                        Fixed32.FromInt(7)))));

        runtime.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromRatio(1, 20),
                    new Tick(1))));

        var worldTransform =
            runtime.EcsWorld.Get<WorldTransform2D>(entity);

        Assert.Equal(
            Fixed32.FromInt(5),
            worldTransform.Position.X);

        Assert.Equal(
            Fixed32.FromInt(7),
            worldTransform.Position.Y);

        Assert.Equal(
            new WorldPosition(5, 7),
            runtime.World.SpatialEntities.GetPosition(entity));
    }

    [Fact]
    public void FixedUpdate_PhysicsMovementPersistsAcrossTicks()
    {
        using var runtime = new EngineRuntime(
            new EngineRuntimeOptions
            {
                ToolingEnabled = false,
                Physics = new PhysicsSettings2D
                {
                    Gravity = FixedVector2.Zero,
                    Substeps = 1
                }
            });

        runtime.Initialize();

        var entity = runtime.World.SpatialEntities.CreateEntity(
            new WorldPosition(10, 0));

        var initialTransform = new FixedTransform2D(
            new FixedVector2(
                Fixed32.FromInt(10),
                Fixed32.Zero));

        runtime.EcsWorld.Add(
            entity,
            new WorldTransform2D(initialTransform.Position));

        runtime.EcsWorld.Add(
            entity,
            new LocalTransform2D(initialTransform));

        var body = PhysicsBody2D.Dynamic(Fixed32.One);

        body.Velocity = new FixedVector2(
            Fixed32.One,
            Fixed32.Zero);

        runtime.EcsWorld.Add(entity, body);

        var delta = new SimulationTime(
            Fixed32.FromRatio(1, 20),
            new Tick(1));

        runtime.FixedUpdate(new FixedSystemContext(delta));

        runtime.FixedUpdate(
            new FixedSystemContext(
                new SimulationTime(
                    Fixed32.FromRatio(1, 20),
                    new Tick(2))));

        var transform =
            runtime.EcsWorld.Get<WorldTransform2D>(entity);

        var localTransform =
    runtime.EcsWorld.Get<LocalTransform2D>(entity);

        Assert.True(
            localTransform.Position.X.ToFloat() >= 10.09f,
            $"Expected local position near 10.1, got {localTransform.Position.X.ToFloat()}");

        Assert.True(
            transform.Position.X.ToFloat() >= 10.09f,
            $"Expected position near 10.1, got {transform.Position.X.ToFloat()}");
    }
}
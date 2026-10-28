using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.Editor.Components;
using Engine.Editor.Inspection;
using Engine.Graphics.Rendering;
using Engine.Graphics.Tilemaps;
using Engine.Serialization.Binary;
using Engine.Serialization.SaveLoad.Ecs;

namespace Engine.Tests.Graphics;

public sealed class TilemapComponentTests
{
    [Fact]
    public void TilemapComponent_RoundTripsThroughEcsWorldSave()
    {
        var registry = new EcsComponentSerializerRegistry();
        registry.RegisterEngineComponents();

        var serializer = new EcsWorldStateSerializer(registry);

        using var world = new World();

        var entity = world.CreateEntity();

        world.Add(
            entity,
            new WorldTransform2D(FixedVector2.Zero));

        world.Add(
            entity,
            new TilemapComponent(
                "Maps/Overworld.tilemap.json",
                17));

        var path = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.vbe");

        try
        {
            BinaryFileSerializer.Save(
                path,
                world.CaptureState(),
                serializer);

            var state = BinaryFileSerializer.Load(
                path,
                serializer);

            using var restoredWorld = new World();
            restoredWorld.RestoreState(state);

            Assert.True(
                restoredWorld.Has<TilemapComponent>(entity));

            var tilemap =
                restoredWorld.Get<TilemapComponent>(entity);

            Assert.Equal(
                "Maps/Overworld.tilemap.json",
                tilemap.TilemapPath);

            Assert.Equal(17, tilemap.Layer);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void RegisterEngineComponents_RegistersTilemapWithDefaults()
    {
        var registry = new EditorComponentTypeRegistry();
        registry.RegisterEngineComponents();

        Assert.True(
            registry.Contains(typeof(TilemapComponent)));

        var tilemap =
            Assert.IsType<TilemapComponent>(
                registry.CreateDefaultValue(
                    typeof(TilemapComponent)));

        Assert.Equal(string.Empty, tilemap.TilemapPath);
        Assert.Equal(RenderLayers.World, tilemap.Layer);
    }

    [Fact]
    public void TilemapComponent_RequiresWorldTransformInEditor()
    {
        var dependencies =
            EditorComponentDependencyRegistry.CreateDefault();

        var missing = dependencies.GetMissingDependencies(
            typeof(TilemapComponent),
            new HashSet<Type>());

        Assert.Contains(
            typeof(WorldTransform2D),
            missing);
    }
}
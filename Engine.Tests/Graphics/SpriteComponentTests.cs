using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.Editor.Components;
using Engine.Editor.Inspection;
using Engine.Graphics2D.Sprites;
using Engine.Serialization.Binary;
using Engine.Serialization.SaveLoad.Ecs;

namespace Engine.Tests.Graphics;

public sealed class SpriteComponentTests
{
    [Fact]
    public void SpriteComponent_RoundTripsThroughEcsWorldSave()
    {
        var registry =
            new EcsComponentSerializerRegistry();

        registry.RegisterEngineComponents();

        var serializer =
            new EcsWorldStateSerializer(registry);

        using var world =
            new World();

        var entity =
            world.CreateEntity();

        world.Add(
            entity,
            new WorldTransform2D(FixedVector2.Zero));

        world.Add(
            entity,
            new SpriteComponent(
                "Textures/player.png",
                3.5f,
                2.25f,
                17));

        var path =
            Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid():N}.vbe");

        try
        {
            BinaryFileSerializer.Save(
                path,
                world.CaptureState(),
                serializer);

            var state =
                BinaryFileSerializer.Load(
                    path,
                    serializer);

            using var restoredWorld =
                new World();

            restoredWorld.RestoreState(state);

            Assert.True(
                restoredWorld.Has<SpriteComponent>(entity));

            var sprite =
                restoredWorld.Get<SpriteComponent>(entity);

            Assert.Equal(
                "Textures/player.png",
                sprite.TexturePath);

            Assert.Equal(3.5f, sprite.Width);
            Assert.Equal(2.25f, sprite.Height);
            Assert.Equal(17, sprite.Layer);
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
    public void RegisterEngineComponents_RegistersSpriteWithDefaults()
    {
        var registry =
            new EditorComponentTypeRegistry();

        registry.RegisterEngineComponents();

        Assert.True(
            registry.Contains(typeof(SpriteComponent)));

        var sprite =
            Assert.IsType<SpriteComponent>(
                registry.CreateDefaultValue(
                    typeof(SpriteComponent)));

        Assert.Equal(string.Empty, sprite.TexturePath);
        Assert.Equal(1.0f, sprite.Width);
        Assert.Equal(1.0f, sprite.Height);
    }

    [Fact]
    public void SpriteComponent_RequiresWorldTransformInEditor()
    {
        var dependencies =
            EditorComponentDependencyRegistry.CreateDefault();

        var missing =
            dependencies.GetMissingDependencies(
                typeof(SpriteComponent),
                new HashSet<Type>());

        Assert.Contains(
            typeof(WorldTransform2D),
            missing);
    }
}
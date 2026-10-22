using Engine.Audio.Components;
using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.Serialization.Binary;
using Engine.Serialization.SaveLoad.Ecs;

namespace Engine.Tests.Audio;

public sealed class AudioComponentTests
{
    [Fact]
    public void AudioComponents_RoundTripThroughEcsWorldSave()
    {
        var registry =
            new EcsComponentSerializerRegistry();

        registry.RegisterEngineComponents();

        var serializer =
            new EcsWorldStateSerializer(registry);

        using var world = new World();

        var sourceEntity = world.CreateEntity();

        world.Add(
            sourceEntity,
            new WorldTransform2D(FixedVector2.Zero));

        world.Add(
            sourceEntity,
            new AudioSourceComponent(
                "Audio/music.wav",
                0.75f,
                1.25f,
                looping: true,
                playOnStart: false));

        var listenerEntity = world.CreateEntity();

        world.Add(
            listenerEntity,
            new WorldTransform2D(FixedVector2.Zero));

        world.Add(
            listenerEntity,
            new AudioListenerComponent(
                0.6f,
                enabled: false));

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

            var source = restoredWorld.Get<AudioSourceComponent>(
                sourceEntity);

            Assert.Equal("Audio/music.wav", source.AudioPath);
            Assert.Equal(0.75f, source.Gain);
            Assert.Equal(1.25f, source.Pitch);
            Assert.True(source.Looping);
            Assert.False(source.PlayOnStart);

            var listener =
                restoredWorld.Get<AudioListenerComponent>(
                    listenerEntity);

            Assert.Equal(0.6f, listener.Gain);
            Assert.False(listener.Enabled);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
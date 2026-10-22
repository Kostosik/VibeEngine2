using Engine.Audio;
using Engine.Audio.Components;
using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.ECS.Entities;
using Engine.Runtime;

namespace Engine.Tests.Audio;

public sealed class AudioEcsSystemTests
{
    [Fact]
    public void Update_CreatesAndUpdatesSpatialAudioSource()
    {
        using var world = new World();
        using var audio = new FakeAudioManager();

        var entity = world.CreateEntity();

        world.Add(
            entity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.FromInt(3),
                    Fixed32.FromInt(4))));

        world.Add(
            entity,
            new AudioSourceComponent(
                "Audio/music.wav",
                gain: 0.5f,
                pitch: 1.25f,
                looping: true,
                playOnStart: true));

        using var system = new AudioEcsSystem(world, audio);

        system.Update(0.0);

        var source = Assert.Single(audio.Sources);

        Assert.Equal(1, source.PlayCount);
        Assert.Equal(0.5f, source.Gain);
        Assert.Equal(1.25f, source.Pitch);
        Assert.True(source.Looping);
        Assert.Equal(
            new Vector3(3.0f, 0.0f, -4.0f),
            source.Position);

        // Перемещение сущности должно обновить позицию
        // и вычисленную скорость источника.
        world.Get<WorldTransform2D>(entity).Position =
            new FixedVector2(
                Fixed32.FromInt(4),
                Fixed32.FromInt(5));

        system.Update(1.0);

        Assert.Equal(
            new Vector3(4.0f, 0.0f, -5.0f),
            source.Position);

        Assert.Equal(
            new Vector3(1.0f, 0.0f, -1.0f),
            source.Velocity);

        // Повторное обновление не должно запускать звук заново.
        Assert.Equal(1, source.PlayCount);
    }

    [Fact]
    public void Update_DisposesSourceAndListenerWhenNoLongerPresent()
    {
        using var world = new World();
        using var audio = new FakeAudioManager();

        var sourceEntity = world.CreateEntity();

        world.Add(
            sourceEntity,
            new WorldTransform2D());

        world.Add(
            sourceEntity,
            new AudioSourceComponent("Audio/effect.wav"));

        var listenerEntity = world.CreateEntity();

        world.Add(
            listenerEntity,
            new WorldTransform2D(
                new FixedVector2(
                    Fixed32.Zero,
                    Fixed32.FromInt(2))));

        world.Add(
            listenerEntity,
            new AudioListenerComponent(
                gain: 0.6f));

        using var system = new AudioEcsSystem(world, audio);

        system.Update(0.016);

        var source = Assert.Single(audio.Sources);
        var listener = Assert.Single(audio.Listeners);

        Assert.Equal(0.6f, listener.Gain);

        Assert.Equal(
            new Vector3(0.0f, 0.0f, -2.0f),
            listener.Position);

        Assert.True(world.Remove<AudioSourceComponent>(sourceEntity));

        system.Update(0.016);

        Assert.True(source.Disposed);

        Assert.True(world.DestroyEntity(listenerEntity));

        // EntityDestroyed должен освободить listener
        // без ожидания следующего Update.
        Assert.True(listener.Disposed);
    }

    [Fact]
    public void Update_RejectsMultipleEnabledListeners()
    {
        using var world = new World();
        using var audio = new FakeAudioManager();

        var first = world.CreateEntity();
        world.Add(first, new WorldTransform2D());
        world.Add(first, new AudioListenerComponent());

        var second = world.CreateEntity();
        world.Add(second, new WorldTransform2D());
        world.Add(second, new AudioListenerComponent());

        using var system = new AudioEcsSystem(world, audio);

        var exception = Assert.Throws<InvalidOperationException>(
            () => system.Update(0.016));

        Assert.Contains(
            "Only one enabled AudioListenerComponent",
            exception.Message);

        Assert.Empty(audio.Listeners);
    }

    private sealed class FakeAudioManager : IAudioManager
    {
        private readonly Dictionary<string, FakeAudioBuffer> _buffers =
            new(StringComparer.Ordinal);

        public List<FakeAudioSource> Sources { get; } = [];

        public List<FakeAudioListener> Listeners { get; } = [];

        public IAudioBuffer Load(AssetPath path)
        {
            if (!_buffers.TryGetValue(path.Value, out var buffer))
            {
                buffer = new FakeAudioBuffer();
                _buffers.Add(path.Value, buffer);
            }

            return buffer;
        }

        public IAudioSource CreateSource(IAudioBuffer buffer)
        {
            var source = new FakeAudioSource();
            Sources.Add(source);
            return source;
        }

        public IAudioSource Play(IAudioBuffer buffer)
        {
            var source = new FakeAudioSource();
            Sources.Add(source);
            source.Play();
            return source;
        }

        public IAudioListener CreateListener()
        {
            var listener = new FakeAudioListener();
            Listeners.Add(listener);
            return listener;
        }

        public void Dispose()
        {
            foreach (var source in Sources)
            {
                source.Dispose();
            }

            foreach (var listener in Listeners)
            {
                listener.Dispose();
            }

            foreach (var buffer in _buffers.Values)
            {
                buffer.Dispose();
            }
        }
    }

    private sealed class FakeAudioBuffer : IAudioBuffer
    {
        public void Dispose()
        {
        }
    }

    private sealed class FakeAudioSource : IAudioSource
    {
        public bool Disposed { get; private set; }

        public int PlayCount { get; private set; }

        public float Gain { get; private set; }

        public float Pitch { get; private set; }

        public bool Looping { get; private set; }

        public Vector3 Position { get; private set; }

        public Vector3 Velocity { get; private set; }

        public void Play()
        {
            PlayCount++;
        }

        public void Pause()
        {
        }

        public void Stop()
        {
        }

        public void Rewind()
        {
        }

        public void SetGain(float gain)
        {
            Gain = gain;
        }

        public void SetPitch(float pitch)
        {
            Pitch = pitch;
        }

        public void SetLooping(bool looping)
        {
            Looping = looping;
        }

        public void SetPosition(Vector3 position)
        {
            Position = position;
        }

        public void SetVelocity(Vector3 velocity)
        {
            Velocity = velocity;
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    private sealed class FakeAudioListener : IAudioListener
    {
        public bool Disposed { get; private set; }

        public float Gain { get; private set; }

        public Vector3 Position { get; private set; }

        public Vector3 Velocity { get; private set; }

        public Vector3 Forward { get; private set; }

        public Vector3 Up { get; private set; }

        public void SetPosition(Vector3 position)
        {
            Position = position;
        }

        public void SetVelocity(Vector3 velocity)
        {
            Velocity = velocity;
        }

        public void SetGain(float gain)
        {
            Gain = gain;
        }

        public void SetOrientation(Vector3 forward, Vector3 up)
        {
            Forward = forward;
            Up = up;
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
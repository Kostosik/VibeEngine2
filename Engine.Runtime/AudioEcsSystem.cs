using System.Runtime.ExceptionServices;
using Engine.Audio;
using Engine.Audio.Components;
using Engine.Core.Assets;
using Engine.Core.Math;
using Engine.ECS;
using Engine.ECS.Components;
using Engine.ECS.Entities;

namespace Engine.Runtime;

public sealed class AudioEcsSystem : IDisposable
{
    private static readonly Vector3 DefaultForward =
        new(0.0f, 0.0f, -1.0f);

    private static readonly Vector3 DefaultUp =
        new(0.0f, 1.0f, 0.0f);

    private readonly World _world;
    private readonly IAudioManager _audio;

    private readonly Dictionary<EntityId, ActiveAudioSource> _sources = new();
    private readonly HashSet<EntityId> _activeSources = new();
    private readonly List<EntityId> _staleSources = new();

    private IAudioListener? _listener;
    private EntityId _listenerEntity;
    private Vector3 _previousListenerPosition;
    private bool _hasPreviousListenerPosition;
    private bool _disposed;

    public AudioEcsSystem(
        World world,
        IAudioManager audio)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(audio);

        _world = world;
        _audio = audio;

        _world.EntityDestroyed += OnEntityDestroyed;
    }

    public void Update(double deltaSeconds)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!double.IsFinite(deltaSeconds) || deltaSeconds < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        }

        var hasListener = TryGetActiveListener(
            out var listenerEntity,
            out var listenerComponent,
            out var listenerTransform);

        if (hasListener)
        {
            ValidateListener(listenerEntity, listenerComponent);
        }

        UpdateSources(deltaSeconds);

        if (hasListener)
        {
            UpdateListener(
                listenerEntity,
                listenerComponent,
                listenerTransform,
                deltaSeconds);
        }
        else
        {
            ResetListener();
        }
    }

    private void UpdateSources(double deltaSeconds)
    {
        _activeSources.Clear();

        foreach (var item in
                 _world.Query<AudioSourceComponent, WorldTransform2D>())
        {
            var component = item.First;
            var transform = item.Second;

            if (string.IsNullOrWhiteSpace(component.AudioPath))
            {
                continue;
            }

            ValidateSource(item.Entity, component);
            _activeSources.Add(item.Entity);

            var position = ToAudioPosition(transform.Position);

            if (_sources.TryGetValue(item.Entity, out var existing))
            {
                if (!string.Equals(
                        existing.AudioPath,
                        component.AudioPath,
                        StringComparison.Ordinal))
                {
                    var replacement = CreateSource(component, position);

                    _sources[item.Entity] = replacement;
                    existing.Source.Dispose();
                }
                else
                {
                    UpdateSource(
                        existing,
                        component,
                        position,
                        deltaSeconds);
                }
            }
            else
            {
                _sources.Add(
                    item.Entity,
                    CreateSource(component, position));
            }
        }

        _staleSources.Clear();

        foreach (var pair in _sources)
        {
            if (!_activeSources.Contains(pair.Key))
            {
                _staleSources.Add(pair.Key);
            }
        }

        foreach (var entity in _staleSources)
        {
            RemoveSource(entity);
        }
    }

    private ActiveAudioSource CreateSource(
        AudioSourceComponent component,
        Vector3 position)
    {
        var buffer = _audio.Load(
            new AssetPath(component.AudioPath));

        var source = _audio.CreateSource(buffer);

        try
        {
            source.SetGain(component.Gain);
            source.SetPitch(component.Pitch);
            source.SetLooping(component.Looping);
            source.SetPosition(position);
            source.SetVelocity(Vector3.Zero);

            if (component.PlayOnStart)
            {
                source.Play();
            }

            return new ActiveAudioSource(
                component.AudioPath,
                source,
                position);
        }
        catch
        {
            try
            {
                source.Dispose();
            }
            catch
            {
                // Сохраняем первоначальную ошибку настройки или запуска.
            }

            throw;
        }
    }

    private static void UpdateSource(
        ActiveAudioSource active,
        AudioSourceComponent component,
        Vector3 position,
        double deltaSeconds)
    {
        var velocity = CalculateVelocity(
            position,
            active.PreviousPosition,
            deltaSeconds);

        active.Source.SetGain(component.Gain);
        active.Source.SetPitch(component.Pitch);
        active.Source.SetLooping(component.Looping);
        active.Source.SetPosition(position);
        active.Source.SetVelocity(velocity);

        active.PreviousPosition = position;
    }

    private bool TryGetActiveListener(
        out EntityId entity,
        out AudioListenerComponent component,
        out WorldTransform2D transform)
    {
        entity = EntityId.Invalid;
        component = default;
        transform = default;

        var found = false;

        foreach (var item in
                 _world.Query<AudioListenerComponent, WorldTransform2D>())
        {
            var candidate = item.First;

            if (!candidate.Enabled)
            {
                continue;
            }

            if (found)
            {
                throw new InvalidOperationException(
                    "Only one enabled AudioListenerComponent " +
                    "may exist at a time.");
            }

            found = true;
            entity = item.Entity;
            component = candidate;
            transform = item.Second;
        }

        return found;
    }

    private void UpdateListener(
        EntityId entity,
        AudioListenerComponent component,
        WorldTransform2D transform,
        double deltaSeconds)
    {
        _listener ??= _audio.CreateListener();

        var position = ToAudioPosition(transform.Position);

        var velocity =
            _hasPreviousListenerPosition &&
            _listenerEntity == entity
                ? CalculateVelocity(
                    position,
                    _previousListenerPosition,
                    deltaSeconds)
                : Vector3.Zero;

        _listener.SetGain(component.Gain);
        _listener.SetPosition(position);
        _listener.SetVelocity(velocity);
        _listener.SetOrientation(DefaultForward, DefaultUp);

        _listenerEntity = entity;
        _previousListenerPosition = position;
        _hasPreviousListenerPosition = true;
    }

    private void ResetListener()
    {
        var listener = _listener;

        _listener = null;
        _listenerEntity = EntityId.Invalid;
        _previousListenerPosition = Vector3.Zero;
        _hasPreviousListenerPosition = false;

        if (listener is null)
        {
            return;
        }

        Exception? firstException = null;

        TryCleanup(
            () => listener.SetPosition(Vector3.Zero),
            ref firstException);

        TryCleanup(
            () => listener.SetVelocity(Vector3.Zero),
            ref firstException);

        TryCleanup(
            () => listener.SetGain(1.0f),
            ref firstException);

        TryCleanup(
            () => listener.SetOrientation(DefaultForward, DefaultUp),
            ref firstException);

        TryCleanup(listener.Dispose, ref firstException);

        ThrowIfFailed(firstException);
    }

    private void RemoveSource(EntityId entity)
    {
        if (_sources.Remove(entity, out var active))
        {
            active.Source.Dispose();
        }
    }

    private void OnEntityDestroyed(EntityId entity)
    {
        RemoveSource(entity);

        if (_listenerEntity == entity)
        {
            ResetListener();
        }
    }

    private static Vector3 ToAudioPosition(FixedVector2 position)
    {
        // Плоскость игры XY отображается на плоскость аудио XZ.
        return new Vector3(
            position.X.ToFloat(),
            0.0f,
            -position.Y.ToFloat());
    }

    private static Vector3 CalculateVelocity(
        Vector3 current,
        Vector3 previous,
        double deltaSeconds)
    {
        if (deltaSeconds <= 0.0)
        {
            return Vector3.Zero;
        }

        return new Vector3(
            (float)((current.X - previous.X) / deltaSeconds),
            (float)((current.Y - previous.Y) / deltaSeconds),
            (float)((current.Z - previous.Z) / deltaSeconds));
    }

    private static void ValidateSource(
        EntityId entity,
        AudioSourceComponent component)
    {
        if (!float.IsFinite(component.Gain) || component.Gain < 0.0f)
        {
            throw new InvalidOperationException(
                $"Audio source on entity '{entity}' has an invalid gain.");
        }

        if (!float.IsFinite(component.Pitch) || component.Pitch <= 0.0f)
        {
            throw new InvalidOperationException(
                $"Audio source on entity '{entity}' has an invalid pitch.");
        }
    }

    private static void ValidateListener(
        EntityId entity,
        AudioListenerComponent component)
    {
        if (!float.IsFinite(component.Gain) || component.Gain < 0.0f)
        {
            throw new InvalidOperationException(
                $"Audio listener on entity '{entity}' has an invalid gain.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _world.EntityDestroyed -= OnEntityDestroyed;

        Exception? firstException = null;

        foreach (var active in _sources.Values)
        {
            TryCleanup(active.Source.Dispose, ref firstException);
        }

        _sources.Clear();
        _activeSources.Clear();
        _staleSources.Clear();

        TryCleanup(ResetListener, ref firstException);

        ThrowIfFailed(firstException);
    }

    private static void TryCleanup(
        Action cleanup,
        ref Exception? firstException)
    {
        try
        {
            cleanup();
        }
        catch (Exception exception)
        {
            firstException ??= exception;
        }
    }

    private static void ThrowIfFailed(Exception? exception)
    {
        if (exception is not null)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    private sealed class ActiveAudioSource
    {
        public ActiveAudioSource(
            string audioPath,
            IAudioSource source,
            Vector3 previousPosition)
        {
            AudioPath = audioPath;
            Source = source;
            PreviousPosition = previousPosition;
        }

        public string AudioPath { get; }

        public IAudioSource Source { get; }

        public Vector3 PreviousPosition { get; set; }
    }
}
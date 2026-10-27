using Engine.Core.Math;
using Silk.NET.OpenAL;

namespace Engine.Audio.OpenAL;

public sealed class OpenALAudioSource : IAudioSource
{
    private readonly OpenALAudioDevice _device;

    private uint _handle;
    private bool _disposed;

    public uint Handle
    {
        get
        {
            ThrowIfDisposed();
            _device.ThrowIfDisposed();

            return _handle;
        }
    }

    public OpenALAudioSource(
        OpenALAudioDevice device,
        IAudioBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(
            device);

        ArgumentNullException.ThrowIfNull(
            buffer);

        _device =
            device;

        _device.ThrowIfDisposed();

        if (buffer is not OpenALAudioBuffer openALBuffer)
        {
            throw new ArgumentException(
                "Audio buffer must belong to the OpenAL audio backend.",
                nameof(buffer));
        }

        if (!openALBuffer.BelongsTo(_device))
        {
            throw new ArgumentException(
                "Audio buffer must belong to the same OpenAL audio device.",
                nameof(buffer));
        }

        _handle =
            _device.AL.GenSource();

        if (_handle == 0)
        {
            throw new InvalidOperationException(
                "Failed to create OpenAL audio source.");
        }

        try
        {
            _device.AL.SetSourceProperty(
                _handle,
                SourceInteger.Buffer,
                openALBuffer.Handle);
        }
        catch (Exception initializationException)
        {
            try
            {
                _device.AL.DeleteSource(
                    _handle);
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException(
                    "Failed to initialize the OpenAL audio source " +
                    "and clean up its resources.",
                    initializationException,
                    cleanupException);
            }
            finally
            {
                _handle = 0;
            }

            throw;
        }
    }

    public void SetBuffer(IAudioBuffer buffer)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(buffer);

        if (buffer is not OpenALAudioBuffer openALBuffer)
        {
            throw new ArgumentException(
                "The audio buffer must be created by the same audio backend.",
                nameof(buffer));
        }

        if (!openALBuffer.BelongsTo(_device))
        {
            throw new ArgumentException(
                "The audio buffer must belong to the same OpenAL audio device.",
                nameof(buffer));
        }

        _device.ThrowIfDisposed();

        _device.AL.SetSourceProperty(
            _handle,
            SourceInteger.Buffer,
            openALBuffer.Handle);
    }

    public void ClearBuffer()
    {
        ThrowIfDisposed();

        _device.AL.SetSourceProperty(
            _handle,
            SourceInteger.Buffer,
            0);
    }

    public void Play()
    {
        ThrowIfDisposed();

        _device.AL.SourcePlay(_handle);
    }

    public void Pause()
    {
        ThrowIfDisposed();

        _device.AL.SourcePause(_handle);
    }

    public void Stop()
    {
        ThrowIfDisposed();

        _device.AL.SourceStop(_handle);
    }

    public void Rewind()
    {
        ThrowIfDisposed();

        _device.AL.SourceRewind(_handle);
    }

    public void SetGain(float gain)
    {
        ThrowIfDisposed();

        if (!float.IsFinite(gain) || gain < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gain),
                "Gain must be finite and non-negative.");
        }

        _device.AL.SetSourceProperty(
            _handle,
            SourceFloat.Gain,
            gain);
    }

    public void SetPitch(float pitch)
    {
        ThrowIfDisposed();

        if (!float.IsFinite(pitch) || pitch <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pitch),
                "Pitch must be finite and greater than zero.");
        }

        _device.AL.SetSourceProperty(
            _handle,
            SourceFloat.Pitch,
            pitch);
    }

    public void SetLooping(bool looping)
    {
        ThrowIfDisposed();

        _device.AL.SetSourceProperty(
            _handle,
            SourceBoolean.Looping,
            looping);
    }

    private static void ValidateVector(
    Vector3 value,
    string parameterName)
    {
        if (!float.IsFinite(value.X) ||
            !float.IsFinite(value.Y) ||
            !float.IsFinite(value.Z))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Vector components must be finite.");
        }
    }

    public void SetPosition(Vector3 position)
    {
        ThrowIfDisposed();

        ValidateVector(
            position,
            nameof(position));

        _device.AL.SetSourceProperty(
            _handle,
            SourceVector3.Position,
            position.X,
            position.Y,
            position.Z);
    }

    public void SetVelocity(Vector3 velocity)
    {
        ThrowIfDisposed();

        ValidateVector(
    velocity,
    nameof(velocity));

        _device.AL.SetSourceProperty(
            _handle,
            SourceVector3.Velocity,
            velocity.X,
            velocity.Y,
            velocity.Z);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (_handle != 0 && !_device.IsDisposed)
            {
                _device.AL.DeleteSource(_handle);
            }
        }
        finally
        {
            _handle = 0;
            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
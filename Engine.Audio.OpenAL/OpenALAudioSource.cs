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
        catch
        {
            _device.AL.DeleteSource(
                _handle);

            _handle = 0;

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

        if (gain < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gain),
                "Gain cannot be negative.");
        }

        _device.AL.SetSourceProperty(
            _handle,
            SourceFloat.Gain,
            gain);
    }

    public void SetPitch(float pitch)
    {
        ThrowIfDisposed();

        if (pitch <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pitch),
                "Pitch must be greater than zero.");
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

    public void SetPosition(Vector3 position)
    {
        ThrowIfDisposed();

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
            return;

        if (_handle != 0 && !_device.IsDisposed)
        {
            _device.AL.DeleteSource(_handle);
            _handle = 0;
        }

        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
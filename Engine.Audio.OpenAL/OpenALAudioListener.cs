using System.Numerics;
using Engine.Core.Math;
using Silk.NET.OpenAL;

namespace Engine.Audio.OpenAL;

public sealed class OpenALAudioListener : IAudioListener
{
    private readonly OpenALAudioDevice _device;

    private bool _disposed;

    public OpenALAudioListener(OpenALAudioDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        _device = device;
        _device.ThrowIfDisposed();
    }

    public void SetPosition(Engine.Core.Math.Vector3 position)
    {
        ThrowIfDisposed();

        var value = new System.Numerics.Vector3(
    position.X,
    position.Y,
    position.Z);

        _device.AL.SetListenerProperty(
            ListenerVector3.Position,
            value);
    }

    public void SetVelocity(Engine.Core.Math.Vector3 velocity)
    {
        ThrowIfDisposed();

        var value = new System.Numerics.Vector3(
        velocity.X,
        velocity.Y,
        velocity.Z);

        _device.AL.SetListenerProperty(
            ListenerVector3.Velocity,
            value);
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

        _device.AL.SetListenerProperty(
            ListenerFloat.Gain,
            gain);
    }

    public void SetOrientation(Engine.Core.Math.Vector3 forward, Engine.Core.Math.Vector3 up)
    {
        ThrowIfDisposed();

        _device.ThrowIfDisposed();



        Span<float> orientation =
        [
            forward.X,
            forward.Y,
            forward.Z,
            up.X,
            up.Y,
            up.Z
        ];

        unsafe
        {
            fixed (float* orientationPtr = orientation)
            {
                _device.AL.SetListenerProperty(
                    ListenerFloatArray.Orientation,
                    orientationPtr);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
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
        ValidateVector(position,nameof(position));
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

        ValidateVector(velocity,nameof(velocity));

        var value = new System.Numerics.Vector3(
        velocity.X,
        velocity.Y,
        velocity.Z);

        _device.AL.SetListenerProperty(
            ListenerVector3.Velocity,
            value);
    }

    private static void ValidateVector(
    Engine.Core.Math.Vector3 value,
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

    public void SetGain(float gain)
    {
        ThrowIfDisposed();

        if (!float.IsFinite(gain) || gain < 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gain),
                "Gain must be finite and non-negative.");
        }

        _device.AL.SetListenerProperty(
            ListenerFloat.Gain,
            gain);
    }

    public void SetOrientation(Engine.Core.Math.Vector3 forward, Engine.Core.Math.Vector3 up)
    {
        ThrowIfDisposed();

        ValidateVector(
    forward,
    nameof(forward));

        ValidateVector(
            up,
            nameof(up));

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
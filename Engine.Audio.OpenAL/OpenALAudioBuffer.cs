using Engine.Audio.Data;
using Silk.NET.OpenAL;

namespace Engine.Audio.OpenAL;

public sealed class OpenALAudioBuffer : IAudioBuffer
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

    public OpenALAudioBuffer(
        OpenALAudioDevice device,
        AudioData data)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(data);

        _device = device;
        _device.ThrowIfDisposed();

        var format = ConvertFormat(data.Format);

        unsafe
        {
            _handle = _device.AL.GenBuffer();

            if (_handle == 0)
            {
                throw new InvalidOperationException(
                    "Failed to create OpenAL audio buffer.");
            }

            try
            {
                var samples = data.Samples.Span;

                fixed (byte* dataPtr = samples)
                {
                    _device.AL.BufferData(
                        _handle,
                        format,
                        dataPtr,
                        samples.Length,
                        data.SampleRate);
                }
            }
            catch
            {
                _device.AL.DeleteBuffer(_handle);
                _handle = 0;

                throw;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_handle != 0 && !_device.IsDisposed)
        {
            _device.AL.DeleteBuffer(_handle);
            _handle = 0;
        }

        _disposed = true;
    }

    private static BufferFormat ConvertFormat(AudioFormat format)
    {
        return format switch
        {
            AudioFormat.Mono8 => BufferFormat.Mono8,
            AudioFormat.Stereo8 => BufferFormat.Stereo8,
            AudioFormat.Mono16 => BufferFormat.Mono16,
            AudioFormat.Stereo16 => BufferFormat.Stereo16,

            _ => throw new ArgumentOutOfRangeException(
                nameof(format),
                format,
                "Unsupported audio format.")
        };
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
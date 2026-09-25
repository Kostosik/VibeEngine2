using Engine.Audio.Data;
using Silk.NET.OpenAL;

namespace Engine.Audio.OpenAL;

public sealed class OpenALAudioDevice : IAudioDevice
{
    private readonly AL _al;
    private readonly ALContext _alc;

    private unsafe Device* _device;
    private unsafe Context* _context;

    private bool _disposed;

    public IAudioBuffer CreateBuffer(AudioData data)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(data);

        return new OpenALAudioBuffer(this, data);
    }

    internal AL AL
    {
        get
        {
            ThrowIfDisposed();
            return _al;
        }
    }

    public bool IsDisposed => _disposed;

    public OpenALAudioDevice()
    {
        _al = AL.GetApi(
    soft: true);
        _alc = ALContext.GetApi();

        try
        {
            unsafe
            {
                _device = _alc.OpenDevice(null);

                if (_device == null)
                {
                    throw new InvalidOperationException(
                        "Failed to open the default OpenAL audio device.");
                }

                _context = _alc.CreateContext(_device, null);

                if (_context == null)
                {
                    throw new InvalidOperationException(
                        "Failed to create the OpenAL audio context.");
                }

                if (!_alc.MakeContextCurrent(_context))
                {
                    throw new InvalidOperationException(
                        "Failed to make the OpenAL audio context current.");
                }
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public IAudioSource CreateSource(
        IAudioBuffer buffer)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(buffer);

        return new OpenALAudioSource(
            this,
            buffer);
    }

    public IAudioListener CreateListener()
    {
        ThrowIfDisposed();

        return new OpenALAudioListener(
            this);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        unsafe
        {
            _alc.MakeContextCurrent(null);

            if (_context != null)
            {
                _alc.DestroyContext(_context);
                _context = null;
            }

            if (_device != null)
            {
                _alc.CloseDevice(_device);
                _device = null;
            }
        }

        _alc.Dispose();
        _al.Dispose();

        _disposed = true;
    }

    internal void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
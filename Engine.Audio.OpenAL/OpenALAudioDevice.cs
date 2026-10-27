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

        _alc = ALContext.GetApi(
            soft: true);

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
            try
            {
                Dispose();
            }
            catch
            {
                // Сохраняем первоначальную ошибку инициализации.
            }

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
        {
            return;
        }

        _disposed = true;

        var errors = new List<Exception>();

        unsafe
        {
            try
            {
                _alc.MakeContextCurrent(null);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }

            if (_context != null)
            {
                var context = _context;
                _context = null;

                try
                {
                    _alc.DestroyContext(context);
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            if (_device != null)
            {
                var device = _device;
                _device = null;

                try
                {
                    _alc.CloseDevice(device);
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }
        }

        try
        {
            _alc.Dispose();
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }

        try
        {
            _al.Dispose();
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }

        if (errors.Count > 0)
        {
            throw new AggregateException(
                "One or more OpenAL resources failed to dispose.",
                errors);
        }
    }

    internal void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
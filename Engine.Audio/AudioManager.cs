using Engine.Audio.Resources;
using Engine.Core.Assets;
using System.Runtime.ExceptionServices;

namespace Engine.Audio;

public sealed class AudioManager :
    IAudioManager
{
    private readonly IAudioDevice _device;
    private readonly IAudioResourceManager _resources;

    private bool _disposed;

    public AudioManager(
        IAssetSource assets,
        IAudioDevice device)
    {
        ArgumentNullException.ThrowIfNull(
            assets);

        ArgumentNullException.ThrowIfNull(
            device);

        _device =
            device;

        _resources =
            new AudioResourceManager(
                assets,
                device);
    }

    public IAudioBuffer Load(
        AssetPath path)
    {
        EnsureNotDisposed();

        return _resources.Load(
            path);
    }

    public IAudioSource CreateSource(
        IAudioBuffer buffer)
    {
        EnsureNotDisposed();

        ArgumentNullException.ThrowIfNull(
            buffer);

        return _device.CreateSource(
            buffer);
    }

    public IAudioSource Play(IAudioBuffer buffer)
    {
        EnsureNotDisposed();
        ArgumentNullException.ThrowIfNull(buffer);

        var source = _device.CreateSource(buffer);

        try
        {
            source.Play();
            return source;
        }
        catch
        {
            try
            {
                source.Dispose();
            }
            catch
            {
                // Сохраняем исходную ошибку воспроизведения.
            }

            throw;
        }
    }

    public IAudioListener CreateListener()
    {
        EnsureNotDisposed();

        return _device.CreateListener();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        Exception? firstException = null;

        try
        {
            _resources.Dispose();
        }
        catch (Exception exception)
        {
            firstException = exception;
        }

        try
        {
            _device.Dispose();
        }
        catch (Exception exception)
        {
            firstException ??= exception;
        }

        if (firstException is not null)
        {
            ExceptionDispatchInfo.Capture(firstException).Throw();
        }
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}
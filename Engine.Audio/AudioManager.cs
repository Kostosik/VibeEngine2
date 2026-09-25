using Engine.Core.Assets;
using Engine.Audio.Resources;

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

    public IAudioSource Play(
        IAudioBuffer buffer)
    {
        EnsureNotDisposed();

        ArgumentNullException.ThrowIfNull(
            buffer);

        var source =
            _device.CreateSource(
                buffer);

        source.Play();

        return source;
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

        _resources.Dispose();
        _device.Dispose();

        _disposed = true;
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}
using Engine.Audio.Loading;
using Engine.Core.Assets;

namespace Engine.Audio.Resources;

public sealed class AudioResourceManager
    : IAudioResourceManager
{
    private readonly IAssetSource _assets;
    private readonly IAudioDevice _audio;
    private readonly AudioLoader _loader;

    private readonly Dictionary<AssetPath, IAudioBuffer> _loaded =
        new();

    private bool _disposed;

    public AudioResourceManager(
        IAssetSource assets,
        IAudioDevice audio)
    {
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(audio);

        _assets = assets;
        _audio = audio;
        _loader = new AudioLoader();
    }

    public IAudioBuffer Load(AssetPath path)
    {
        EnsureNotDisposed();
        EnsurePathValid(path);

        if (_loaded.TryGetValue(path, out var existing))
            return existing;

        var data =
            _loader.Load(
                _assets,
                path);

        var buffer =
            _audio.CreateBuffer(data);

        _loaded.Add(
            path,
            buffer);

        return buffer;
    }

    public bool IsLoaded(AssetPath path)
    {
        EnsureNotDisposed();

        return _loaded.ContainsKey(path);
    }

    public bool TryGet(
        AssetPath path,
        out IAudioBuffer buffer)
    {
        EnsureNotDisposed();

        return _loaded.TryGetValue(
            path,
            out buffer!);
    }

    public bool Unload(AssetPath path)
    {
        EnsureNotDisposed();

        if (!_loaded.Remove(
                path,
                out var buffer))
        {
            return false;
        }

        buffer.Dispose();

        return true;
    }

    public void UnloadAll()
    {
        EnsureNotDisposed();

        foreach (var buffer in _loaded.Values)
        {
            buffer.Dispose();
        }

        _loaded.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var buffer in _loaded.Values)
        {
            buffer.Dispose();
        }

        _loaded.Clear();

        _disposed = true;
    }

    private static void EnsurePathValid(
        AssetPath path)
    {
        if (string.IsNullOrWhiteSpace(path.Value))
        {
            throw new ArgumentException(
                "Asset path must be valid.",
                nameof(path));
        }
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}
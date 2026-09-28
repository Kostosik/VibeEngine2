using Engine.Graphics.OpenGL.Resources;
using Engine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL.Rendering;

internal sealed class OpenGLRenderTargetManager :
    IRenderTargetManager,
    IDisposable
{
    private sealed class Entry
    {
        public required uint Framebuffer
        {
            get;
            init;
        }

        public required TextureHandle ColorTexture
        {
            get;
            init;
        }

        public required RenderTargetDescription Description
        {
            get;
            init;
        }
    }

    private readonly GL _gl;
    private readonly OpenGLTextureManager _textures;

    private readonly Dictionary<
        RenderTargetHandle,
        Entry> _targets =
        new();

    private uint _nextHandle = 1;

    private bool _disposed;

    public OpenGLRenderTargetManager(
        GL gl,
        OpenGLTextureManager textures)
    {
        ArgumentNullException.ThrowIfNull(
            gl);

        ArgumentNullException.ThrowIfNull(
            textures);

        _gl = gl;
        _textures = textures;
    }

    public RenderTargetHandle Create(
        RenderTargetDescription description)
    {
        ThrowIfDisposed();

        description.Validate();

        var colorTexture =
            _textures.Create(
                new TextureDescription(
                    description.Width,
                    description.Height,
                    description.Format));

        var framebuffer =
            _gl.GenFramebuffer();

        if (framebuffer == 0)
        {
            _textures.Destroy(
                colorTexture);

            throw new InvalidOperationException(
                "OpenGL failed to create a framebuffer.");
        }

        _gl.BindFramebuffer(
            FramebufferTarget.Framebuffer,
            framebuffer);

        _gl.FramebufferTexture2D(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D,
            colorTexture.Value,
            0);

        var status =
            _gl.CheckFramebufferStatus(
                FramebufferTarget.Framebuffer);

        _gl.BindFramebuffer(
            FramebufferTarget.Framebuffer,
            0);

        if (status !=
            GLEnum.FramebufferComplete)
        {
            _gl.DeleteFramebuffer(
                framebuffer);

            _textures.Destroy(
                colorTexture);

            throw new InvalidOperationException(
                $"OpenGL framebuffer is incomplete: {status}.");
        }

        var handle =
            CreateHandle();

        _targets.Add(
            handle,
            new Entry
            {
                Framebuffer =
                    framebuffer,

                ColorTexture =
                    colorTexture,

                Description =
                    description
            });

        return handle;
    }

    public bool Exists(
        RenderTargetHandle target)
    {
        ThrowIfDisposed();

        return target.IsValid &&
               _targets.ContainsKey(
                   target);
    }

    public RenderTargetDescription GetDescription(
        RenderTargetHandle target)
    {
        ThrowIfDisposed();

        return GetEntry(
            target)
            .Description;
    }

    public TextureHandle GetColorTexture(
        RenderTargetHandle target)
    {
        ThrowIfDisposed();

        return GetEntry(
            target)
            .ColorTexture;
    }

    public void Resize(
    RenderTargetHandle target,
    int width,
    int height)
    {
        ThrowIfDisposed();

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height));
        }

        var entry =
            GetEntry(target);

        if (entry.Description.Width == width &&
            entry.Description.Height == height)
        {
            return;
        }

        var description =
            new RenderTargetDescription(
                width,
                height,
                entry.Description.Format);

        var newColorTexture =
            _textures.Create(
                new TextureDescription(
                    description.Width,
                    description.Height,
                    description.Format));

        var newFramebuffer =
            _gl.GenFramebuffer();

        if (newFramebuffer == 0)
        {
            _textures.Destroy(
                newColorTexture);

            throw new InvalidOperationException(
                "OpenGL failed to create a framebuffer.");
        }

        _gl.BindFramebuffer(
            FramebufferTarget.Framebuffer,
            newFramebuffer);

        _gl.FramebufferTexture2D(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D,
            newColorTexture.Value,
            0);

        var status =
            _gl.CheckFramebufferStatus(
                FramebufferTarget.Framebuffer);

        _gl.BindFramebuffer(
            FramebufferTarget.Framebuffer,
            0);

        if (status !=
            GLEnum.FramebufferComplete)
        {
            _gl.DeleteFramebuffer(
                newFramebuffer);

            _textures.Destroy(
                newColorTexture);

            throw new InvalidOperationException(
                $"OpenGL framebuffer is incomplete: {status}.");
        }

        _targets[target] =
            new Entry
            {
                Framebuffer =
                    newFramebuffer,

                ColorTexture =
                    newColorTexture,

                Description =
                    description
            };

        _gl.DeleteFramebuffer(
            entry.Framebuffer);

        _textures.Destroy(
            entry.ColorTexture);
    }
    public void Destroy(
        RenderTargetHandle target)
    {
        ThrowIfDisposed();

        if (!_targets.Remove(
                target,
                out var entry))
        {
            return;
        }

        _gl.DeleteFramebuffer(
            entry.Framebuffer);

        _textures.Destroy(
            entry.ColorTexture);
    }

    internal uint GetFramebuffer(
        RenderTargetHandle target)
    {
        ThrowIfDisposed();

        return GetEntry(
            target)
            .Framebuffer;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var entry in _targets.Values)
        {
            _gl.DeleteFramebuffer(
                entry.Framebuffer);

            _textures.Destroy(
                entry.ColorTexture);
        }

        _targets.Clear();

        _disposed = true;
    }

    private RenderTargetHandle CreateHandle()
    {
        while (_nextHandle == 0 ||
               _targets.ContainsKey(
                   new RenderTargetHandle(
                       _nextHandle)))
        {
            _nextHandle++;

            if (_nextHandle == 0)
            {
                throw new InvalidOperationException(
                    "Render target handle space is exhausted.");
            }
        }

        return new RenderTargetHandle(
            _nextHandle++);
    }

    private Entry GetEntry(
        RenderTargetHandle target)
    {
        if (!target.IsValid ||
            !_targets.TryGetValue(
                target,
                out var entry))
        {
            throw new KeyNotFoundException(
                $"Render target '{target.Value}' was not found.");
        }

        return entry;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }
}
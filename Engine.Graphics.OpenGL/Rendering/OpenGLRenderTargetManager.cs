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
        public required uint DepthBuffer
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

    private uint CreateDepthBuffer(int width, int height)
    {
        var handle = _gl.GenRenderbuffer();

        if (handle == 0)
        {
            throw new InvalidOperationException(
                "OpenGL failed to create a depth buffer.");
        }

        try
        {
            _gl.BindRenderbuffer(
                RenderbufferTarget.Renderbuffer,
                handle);

            _gl.RenderbufferStorage(
                RenderbufferTarget.Renderbuffer,
                InternalFormat.DepthComponent24,
                (uint)width,
                (uint)height);

            return handle;
        }
        catch
        {
            _gl.DeleteRenderbuffer(handle);
            throw;
        }
        finally
        {
            _gl.BindRenderbuffer(
                RenderbufferTarget.Renderbuffer,
                0);
        }
    }

    private void AttachDepthBuffer(uint depthBuffer)
    {
        _gl.FramebufferRenderbuffer(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment,
            RenderbufferTarget.Renderbuffer,
            depthBuffer);
    }

    public RenderTargetHandle Create(
    RenderTargetDescription description)
    {
        ThrowIfDisposed();
        description.Validate();

        var colorTexture = _textures.Create(
            new TextureDescription(
                description.Width,
                description.Height,
                description.Format));

        uint framebuffer = 0;
        uint depthBuffer = 0;

        try
        {
            framebuffer = _gl.GenFramebuffer();

            if (framebuffer == 0)
            {
                throw new InvalidOperationException(
                    "OpenGL failed to create a framebuffer.");
            }

            depthBuffer = CreateDepthBuffer(
                description.Width,
                description.Height);

            GLEnum status;

            try
            {
                _gl.BindFramebuffer(
                    FramebufferTarget.Framebuffer,
                    framebuffer);

                _gl.FramebufferTexture2D(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.ColorAttachment0,
                    TextureTarget.Texture2D,
                    colorTexture.Value,
                    0);

                AttachDepthBuffer(depthBuffer);

                status = _gl.CheckFramebufferStatus(
                    FramebufferTarget.Framebuffer);
            }
            finally
            {
                _gl.BindFramebuffer(
                    FramebufferTarget.Framebuffer,
                    0);
            }

            if (status != GLEnum.FramebufferComplete)
            {
                throw new InvalidOperationException(
                    $"OpenGL framebuffer is incomplete: {status}.");
            }

            var handle = CreateHandle();

            _targets.Add(
                handle,
                new Entry
                {
                    Framebuffer = framebuffer,
                    ColorTexture = colorTexture,
                    DepthBuffer = depthBuffer,
                    Description = description
                });

            return handle;
        }
        catch
        {
            if (framebuffer != 0)
            {
                _gl.DeleteFramebuffer(framebuffer);
            }

            if (depthBuffer != 0)
            {
                _gl.DeleteRenderbuffer(depthBuffer);
            }

            _textures.Destroy(colorTexture);

            throw;
        }
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

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        var entry = GetEntry(target);

        if (entry.Description.Width == width &&
            entry.Description.Height == height)
        {
            return;
        }

        var description = new RenderTargetDescription(
            width,
            height,
            entry.Description.Format);

        description.Validate();

        var newColorTexture = _textures.Create(
            new TextureDescription(
                description.Width,
                description.Height,
                description.Format));

        uint newFramebuffer = 0;
        uint newDepthBuffer = 0;

        try
        {
            newFramebuffer = _gl.GenFramebuffer();

            if (newFramebuffer == 0)
            {
                throw new InvalidOperationException(
                    "OpenGL failed to create a framebuffer.");
            }

            newDepthBuffer = CreateDepthBuffer(width, height);

            GLEnum status;

            try
            {
                _gl.BindFramebuffer(
                    FramebufferTarget.Framebuffer,
                    newFramebuffer);

                _gl.FramebufferTexture2D(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.ColorAttachment0,
                    TextureTarget.Texture2D,
                    newColorTexture.Value,
                    0);

                AttachDepthBuffer(newDepthBuffer);

                status = _gl.CheckFramebufferStatus(
                    FramebufferTarget.Framebuffer);
            }
            finally
            {
                _gl.BindFramebuffer(
                    FramebufferTarget.Framebuffer,
                    0);
            }

            if (status != GLEnum.FramebufferComplete)
            {
                throw new InvalidOperationException(
                    $"OpenGL framebuffer is incomplete: {status}.");
            }

            _targets[target] = new Entry
            {
                Framebuffer = newFramebuffer,
                ColorTexture = newColorTexture,
                DepthBuffer = newDepthBuffer,
                Description = description
            };

            newFramebuffer = 0;
            newDepthBuffer = 0;
        }
        catch
        {
            if (newFramebuffer != 0)
            {
                _gl.DeleteFramebuffer(newFramebuffer);
            }

            if (newDepthBuffer != 0)
            {
                _gl.DeleteRenderbuffer(newDepthBuffer);
            }

            _textures.Destroy(newColorTexture);

            throw;
        }

        // Освобождаем старые ресурсы только после успешной замены.
        _gl.DeleteFramebuffer(entry.Framebuffer);
        _gl.DeleteRenderbuffer(entry.DepthBuffer);
        _textures.Destroy(entry.ColorTexture);
    }
    public void Destroy(
        RenderTargetHandle target)
    {
        ThrowIfDisposed();

        if (!_targets.TryGetValue(
                target,
                out var entry))
        {
            return;
        }

        var errors = new List<Exception>();

        try
        {
            _gl.DeleteFramebuffer(
                entry.Framebuffer);
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }

        try
        {
            _textures.Destroy(
                entry.ColorTexture);
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }
        try
        {
            _gl.DeleteRenderbuffer(entry.DepthBuffer);
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }
        if (errors.Count > 0)
        {
            throw new AggregateException(
                $"Render target '{target.Value}' could not be fully released.",
                errors);
        }

        _targets.Remove(
            target);
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

        var errors = new List<Exception>();

        foreach (var entry in _targets.Values)
        {
            try
            {
                _gl.DeleteFramebuffer(
                    entry.Framebuffer);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
            try
            {
                _gl.DeleteRenderbuffer(entry.DepthBuffer);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
            try
            {
                _textures.Destroy(
                    entry.ColorTexture);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        _targets.Clear();
        _disposed = true;

        if (errors.Count > 0)
        {
            throw new AggregateException(
                "One or more OpenGL render target resources could not be released.",
                errors);
        }
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
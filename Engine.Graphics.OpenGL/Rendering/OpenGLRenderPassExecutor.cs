
using Engine.Graphics.Commands;
using Engine.Graphics.Rendering;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL.Rendering;

internal sealed class OpenGLRenderPassExecutor :
    IRenderPassExecutor
{
    private readonly GL _gl;
    private readonly OpenGLRenderTargetManager _renderTargets;
    private readonly OpenGLRenderCommandDispatcher _dispatcher;

    // Логический размер backbuffer.
    private int _backBufferRenderWidth;
    private int _backBufferRenderHeight;

    // Физический размер framebuffer.
    private int _backBufferFramebufferWidth;
    private int _backBufferFramebufferHeight;

    public OpenGLRenderPassExecutor(
        GL gl,
        OpenGLRenderTargetManager renderTargets,
        OpenGLRenderCommandDispatcher dispatcher,
        int renderWidth,
        int renderHeight,
        int framebufferWidth,
        int framebufferHeight)
    {
        ArgumentNullException.ThrowIfNull(gl);
        ArgumentNullException.ThrowIfNull(renderTargets);
        ArgumentNullException.ThrowIfNull(dispatcher);

        ValidateSize(renderWidth, renderHeight);
        ValidateSize(framebufferWidth, framebufferHeight);

        _gl = gl;
        _renderTargets = renderTargets;
        _dispatcher = dispatcher;

        _backBufferRenderWidth = renderWidth;
        _backBufferRenderHeight = renderHeight;

        _backBufferFramebufferWidth = framebufferWidth;
        _backBufferFramebufferHeight = framebufferHeight;
    }

    public void Begin(RenderPassContext context)
    {
        var framebuffer = 0;

        // По умолчанию используем размеры главного окна.
        var viewportWidth = _backBufferFramebufferWidth;
        var viewportHeight = _backBufferFramebufferHeight;

        var renderWidth = _backBufferRenderWidth;
        var renderHeight = _backBufferRenderHeight;

        // Для отдельного RenderTarget размеры совпадают
        // с его собственным разрешением.
        if (context.Target.IsValid)
        {
            framebuffer = (int)_renderTargets.GetFramebuffer(
                context.Target);

            var description = _renderTargets.GetDescription(
                context.Target);

            viewportWidth = description.Width;
            viewportHeight = description.Height;

            renderWidth = description.Width;
            renderHeight = description.Height;
        }

        var frameStarted = false;

        try
        {
            _gl.BindFramebuffer(
                FramebufferTarget.Framebuffer,
                (uint)framebuffer);

            // Viewport всегда использует реальные пиксели.
            _gl.Viewport(
                0,
                0,
                (uint)viewportWidth,
                (uint)viewportHeight);

            // Координаты шейдеров используют логический размер.
            _dispatcher.SetRenderSize(
                renderWidth,
                renderHeight);

            if (context.ClearColor)
            {
                _gl.Clear(
                    ClearBufferMask.ColorBufferBit);
            }

            _dispatcher.BeginFrame();
            frameStarted = true;

            _dispatcher.ApplyState(context.State);
        }
        catch
        {
            if (frameStarted)
            {
                try
                {
                    _dispatcher.EndFrame();
                }
                catch
                {
                    // Сохраняем исходную ошибку Begin().
                }
            }

            try
            {
                _gl.BindFramebuffer(
                    FramebufferTarget.Framebuffer,
                    0);
            }
            catch
            {
                // Сохраняем исходную ошибку Begin().
            }

            throw;
        }
    }

    public void Execute(IRenderCommand command)
    {
        _dispatcher.Execute(command);
    }

    public void Execute(DrawWorldTextureData command)
    {
        _dispatcher.Execute(command);
    }

    public void End()
    {
        try
        {
            _dispatcher.EndFrame();
        }
        finally
        {
            _gl.BindFramebuffer(
                FramebufferTarget.Framebuffer,
                0);
        }
    }

    public void Resize(
        int renderWidth,
        int renderHeight,
        int framebufferWidth,
        int framebufferHeight)
    {
        ValidateSize(renderWidth, renderHeight);
        ValidateSize(framebufferWidth, framebufferHeight);

        _backBufferRenderWidth = renderWidth;
        _backBufferRenderHeight = renderHeight;

        _backBufferFramebufferWidth = framebufferWidth;
        _backBufferFramebufferHeight = framebufferHeight;
    }

    private static void ValidateSize(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
    }
}

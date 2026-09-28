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

    private int _backBufferWidth;
    private int _backBufferHeight;

    public OpenGLRenderPassExecutor(
        GL gl,
        OpenGLRenderTargetManager renderTargets,
        OpenGLRenderCommandDispatcher dispatcher,
        int width,
        int height)
    {
        _gl = gl;
        _renderTargets = renderTargets;
        _dispatcher = dispatcher;

        _backBufferWidth = width;
        _backBufferHeight = height;
    }

    public void Begin(
        RenderPassContext context)
    {
        var framebuffer = 0;

        var width =
            _backBufferWidth;

        var height =
            _backBufferHeight;

        if (context.Target.IsValid)
        {
            framebuffer =
                (int)_renderTargets.GetFramebuffer(
                    context.Target);

            var description =
                _renderTargets.GetDescription(
                    context.Target);

            width =
                description.Width;

            height =
                description.Height;

            
        }

        _gl.BindFramebuffer(
            FramebufferTarget.Framebuffer,
            (uint)framebuffer);

        _gl.Viewport(
            0,
            0,
            (uint)width,
            (uint)height);

        _dispatcher.SetRenderSize(width, height);

        if (context.ClearColor)
        {
            _gl.Clear(
                ClearBufferMask.ColorBufferBit);
        }

        _dispatcher.BeginFrame();

        _dispatcher.ApplyState(
            context.State);
    }

    public void Execute(
        IRenderCommand command)
    {
        _dispatcher.Execute(
            command);
    }

    public void End()
    {
        _dispatcher.EndFrame();

        _gl.BindFramebuffer(
            FramebufferTarget.Framebuffer,
            0);
    }

    public void Resize(
        int width,
        int height)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(width));

        if (height <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(height));

        _backBufferWidth = width;
        _backBufferHeight = height;
    }
}
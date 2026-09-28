using Engine.Graphics.Cameras;
using Engine.Graphics.Commands;
using Engine.Graphics.Fonts;
using Engine.Graphics.OpenGL.Fonts;
using Engine.Graphics.OpenGL.Rendering;
using Engine.Graphics.OpenGL.Resources;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL;

public sealed class OpenGLGraphicsDevice
    : IGraphicsDevice, IDisposable
{
    private readonly GL _gl;

    private readonly OpenGLDebugRenderer _debugRenderer;
    private readonly OpenGLTextureManager _textureManager;
    private readonly OpenGLTextureRenderer _textureRenderer;

    private readonly OpenGLFontManager _fontManager;
    private readonly OpenGLFontRenderer _fontRenderer;

    private readonly OpenGLGraphicsBufferManager _bufferManager;
    private readonly OpenGLShaderManager _shaderManager;

    private readonly OpenGLRenderState _renderState;
    private readonly OpenGLRenderTargetManager _renderTargetManager;

    private readonly OpenGLRenderCommandDispatcher _dispatcher;
    private readonly OpenGLRenderPassExecutor _passExecutor;

    private readonly RenderQueue _renderQueue = new();
    private readonly RenderPipeline _renderPipeline;

    public RenderPipeline Pipeline =>
    _renderPipeline;
    public ITextureManager Textures =>
        _textureManager;

    public IFontManager Fonts =>
        _fontManager;

    public IGraphicsBufferManager Buffers =>
        _bufferManager;

    public IShaderManager Shaders =>
        _shaderManager;

    public IRenderTargetManager RenderTargets =>
        _renderTargetManager;

    public OpenGLGraphicsDevice(
        GL gl,
        int width,
        int height,
        Camera camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        _gl = gl;

        _renderState =
            new OpenGLRenderState(
                gl);

        _textureManager =
            new OpenGLTextureManager(
                gl);

        _bufferManager =
            new OpenGLGraphicsBufferManager(
                gl);

        _shaderManager =
            new OpenGLShaderManager(
                gl);

        _textureRenderer =
            new OpenGLTextureRenderer(
                gl,
                width,
                height,
                camera);

        _debugRenderer =
            new OpenGLDebugRenderer(
                gl,
                width,
                height,
                camera);

        _fontManager =
            new OpenGLFontManager(
                _textureManager);

        _fontRenderer =
            new OpenGLFontRenderer(
                gl,
                width,
                height,
                _fontManager);

        _renderTargetManager =
            new OpenGLRenderTargetManager(
                gl,
                _textureManager);

        _dispatcher =
            new OpenGLRenderCommandDispatcher(
                _textureManager,
                _textureRenderer,
                _debugRenderer,
                _fontRenderer,
                _renderState);

        _passExecutor =
            new OpenGLRenderPassExecutor(
                gl,
                _renderTargetManager,
                _dispatcher,
                width,
                height);

        _renderPipeline =
            new RenderPipeline();

        _renderPipeline.AddPass(
            RenderPass.Default2D);

        _renderPipeline.AddPass(
    RenderPass.World2D);

        _renderPipeline.AddPass(
            RenderPass.Ui);

        _renderPipeline.AddPass(
            RenderPass.Debug);

        Configure();
    }

    public void BeginFrame()
    {
        _renderQueue.Clear();
    }

    public void Submit(
        IRenderCommand command)
    {
        _renderQueue.Submit(
            command);
    }

    public void EndFrame()
    {
        _renderPipeline.Execute(
            _renderQueue,
            _passExecutor);
    }

    public void Resize(
        int width,
        int height)
    {
        _passExecutor.Resize(
            width,
            height);
    }

    public void Dispose()
    {
        _debugRenderer.Dispose();
        _textureRenderer.Dispose();
        _textureManager.Dispose();
        _fontManager.Dispose();
        _bufferManager.Dispose();
        _shaderManager.Dispose();
        _renderTargetManager.Dispose();
    }

    private void Configure()
    {
        _gl.ClearColor(
            0.05f,
            0.05f,
            0.05f,
            1.0f);
    }
}
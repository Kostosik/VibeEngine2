using Engine.Camera;
using Engine.Graphics.Commands;
using Engine.Graphics.Fonts;
using Engine.Graphics.OpenGL.Fonts;
using Engine.Graphics.OpenGL.Rendering;
using Engine.Graphics.OpenGL.Resources;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;
using Engine.Graphics2D.Rendering;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL;

public sealed class OpenGLGraphicsDevice
    : IGraphicsDevice, IDisposable
{
    private readonly GL _gl;
    private bool _disposed;
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
        Camera2D camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        _gl = gl;

        try
        {
            _renderState =
                new OpenGLRenderState(gl);

            _textureManager =
                new OpenGLTextureManager(gl);

            _bufferManager =
                new OpenGLGraphicsBufferManager(gl);

            _shaderManager =
                new OpenGLShaderManager(gl);

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
                    _renderState,
                    _renderTargetManager);

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
                RenderPass2D.World);

            _renderPipeline.AddPass(
                RenderPass2D.Ui);

            _renderPipeline.AddPass(
                RenderPass2D.Debug);

            Configure();
        }
        catch
        {
            TryDispose(_fontRenderer);
            TryDispose(_textureRenderer);
            TryDispose(_debugRenderer);

            TryDispose(_fontManager);
            TryDispose(_renderTargetManager);
            TryDispose(_textureManager);

            TryDispose(_bufferManager);
            TryDispose(_shaderManager);

            throw;
        }
    }

    private static void TryDispose(
    IDisposable? resource)
    {
        try
        {
            resource?.Dispose();
        }
        catch
        {
            // Не подменяем исходную ошибку конструктора.
        }
    }

    public void BeginFrame()
    {
        ThrowIfDisposed();
        _renderQueue.Clear();
    }

    public void Submit(
        IRenderCommand command)
    {
        ThrowIfDisposed();
        _renderQueue.Submit(
            command);
    }

    public TimeSpan LastEndFrameTime { get; private set; }

    public void EndFrame()
    {
        ThrowIfDisposed();
        _renderPipeline.Execute(
            _renderQueue,
            _passExecutor);
    }

    public void Resize(
        int width,
        int height)
    {
        ThrowIfDisposed();
        _passExecutor.Resize(
            width,
            height);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(
                nameof(OpenGLGraphicsDevice));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        var errors = new List<Exception>();

        DisposeResource(_debugRenderer, errors);
        DisposeResource(_textureRenderer, errors);
        DisposeResource(_fontRenderer, errors);

        DisposeResource(_fontManager, errors);
        DisposeResource(_renderTargetManager, errors);
        DisposeResource(_textureManager, errors);

        DisposeResource(_bufferManager, errors);
        DisposeResource(_shaderManager, errors);

        if (errors.Count > 0)
        {
            throw new AggregateException(
                "One or more graphics resources failed to dispose.",
                errors);
        }
    }

    private static void DisposeResource(
    IDisposable resource,
    List<Exception> errors)
    {
        try
        {
            resource.Dispose();
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }
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
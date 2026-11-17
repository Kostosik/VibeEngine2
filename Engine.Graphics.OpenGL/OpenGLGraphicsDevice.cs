using Engine.Camera;
using Engine.Graphics.Commands;
using Engine.Graphics.Fonts;
using Engine.Graphics.OpenGL.Fonts;
using Engine.Graphics.OpenGL.Rendering;
using Engine.Graphics.OpenGL.Resources;
using Engine.Graphics.Rendering;
using Engine.Graphics.Rendering.RenderGraph;
using Engine.Graphics.Resources;
using Engine.Graphics2D.Rendering;
using Engine.Graphics3D.Resources;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL;

public sealed class OpenGLGraphicsDevice
    : IGraphicsDevice,
      IWorldTextureDataSubmitter,
      IMeshManagerProvider
{
    private RenderTargetHandle _sceneColorTarget =
    RenderTargetHandle.Invalid;
    private readonly RenderGraphExecutor _renderGraphExecutor;

    private RenderGraphExecutionPlan _renderGraphPlan = null!;
    private int _logicalWidth;
    private int _logicalHeight;

    private long _compiledPipelineRevision = -1;
    private readonly OpenGLFullscreenRenderer _fullscreenRenderer;
    private readonly GL _gl;
    private bool _disposed;
    private readonly OpenGLDebugRenderer _debugRenderer;
    private readonly OpenGLTextureManager _textureManager;
    private readonly OpenGLTextureRenderer _textureRenderer;

    private readonly OpenGLFontManager _fontManager;
    private readonly OpenGLFontRenderer _fontRenderer;

    private readonly OpenGLGraphicsBufferManager _bufferManager;
    private readonly OpenGLShaderManager _shaderManager;
    private readonly OpenGLMeshManager _meshManager;
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
    public IMeshManager Meshes => _meshManager;
    public IShaderManager Shaders =>
        _shaderManager;

    public IRenderTargetManager RenderTargets =>
        _renderTargetManager;

    public OpenGLGraphicsDevice(
        GL gl,
        int width,
        int height,
        Camera2D camera)
        : this(
            gl,
            width,
            height,
            width,
            height,
            camera)
    {
    }

    public OpenGLGraphicsDevice(
        GL gl,
        int logicalWidth,
        int logicalHeight,
        int framebufferWidth,
        int framebufferHeight,
        Camera2D camera)
    {
        ArgumentNullException.ThrowIfNull(gl);
        ArgumentNullException.ThrowIfNull(camera);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(logicalWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(logicalHeight);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(framebufferWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(framebufferHeight);

        _gl = gl;
        _logicalWidth = logicalWidth;
        _logicalHeight = logicalHeight;
        try
        {
            _renderState =
                new OpenGLRenderState(gl);

            _textureManager =
                new OpenGLTextureManager(gl);

            _bufferManager =
                new OpenGLGraphicsBufferManager(gl);
            _meshManager = new OpenGLMeshManager(
                gl,
                _bufferManager);
            _shaderManager =
                new OpenGLShaderManager(gl);

            _textureRenderer =
                new OpenGLTextureRenderer(
                    gl,
                    logicalWidth,
                    logicalHeight,
                    camera);

            _debugRenderer =
                new OpenGLDebugRenderer(
                    gl,
                    logicalWidth,
                    logicalHeight,
                    camera);

            _fontManager =
                new OpenGLFontManager(
                    _textureManager);

            _fontRenderer =
                new OpenGLFontRenderer(
                    gl,
                    logicalWidth,
                    logicalHeight,
                    _fontManager);

            _renderTargetManager =
                new OpenGLRenderTargetManager(
                    gl,
                    _textureManager);
            _sceneColorTarget = _renderTargetManager.Create(
                new RenderTargetDescription(
                    logicalWidth,
                    logicalHeight));

            _fullscreenRenderer =
                new OpenGLFullscreenRenderer(gl);
            _dispatcher =
                new OpenGLRenderCommandDispatcher(
                    _textureManager,
                    _textureRenderer,
                    _debugRenderer,
                    _fontRenderer,
                    _renderState,
                    _renderTargetManager,
                    _shaderManager,
                    _fullscreenRenderer, 
                    new OpenGLMeshRenderer(
                        gl,
                        _meshManager,
                        _shaderManager));

            _passExecutor =
                new OpenGLRenderPassExecutor(
                    gl,
                    _renderTargetManager,
                    _dispatcher,
                    _shaderManager,
                    _fullscreenRenderer,
                    logicalWidth,
                    logicalHeight,
                    framebufferWidth,
                    framebufferHeight);

            _renderPipeline =
                new RenderPipeline();

            _renderPipeline.AddPass(
                RenderPass2D.World);

            _renderPipeline.AddPass(
                RenderPass2D.Ui);

            _renderPipeline.AddPass(
                RenderPass2D.Debug);

            _renderGraphExecutor = new RenderGraphExecutor(
                _renderTargetManager,
                _passExecutor);

            _renderGraphPlan = BuildRenderGraph(
                logicalWidth,
                logicalHeight);
            _compiledPipelineRevision = _renderPipeline.Revision;
            Configure();
        }
        catch
        {

            TryDispose(_fontRenderer);
            TryDispose(_textureRenderer);
            TryDispose(_debugRenderer);
            TryDispose(_passExecutor);
            TryDispose(_fullscreenRenderer);
            TryDispose(_fontManager);
            TryDispose(_renderGraphExecutor);
            TryDispose(_renderTargetManager);
            TryDispose(_textureManager);
            TryDispose(_meshManager);
            TryDispose(_bufferManager);
            TryDispose(_shaderManager);
            TryDispose(gl);
            throw;
        }
    }
    private RenderGraphExecutionPlan BuildRenderGraph(
        int logicalWidth,
        int logicalHeight)
    {
        ValidateRenderPipeline();

        var builder = new RenderGraphBuilder();

        var sceneColor = builder.ImportRenderTarget(
            "SceneColor",
            _sceneColorTarget,
            new RenderTargetDescription(
                logicalWidth,
                logicalHeight));

        var backbuffer = builder.ImportBackbuffer(
            "Backbuffer",
            new RenderTargetDescription(
                logicalWidth,
                logicalHeight));

        // Повторно используем одно описание ресурса,
        // если несколько проходов пишут в один RenderTarget.
        var pipelineTargets =
            new Dictionary<RenderTargetHandle, RenderGraphResourceHandle>
            {
                [_sceneColorTarget] = sceneColor
            };

        RenderGraphResourceHandle ResolveTarget(RenderPass renderPass)
        {
            // Старый API использует Invalid для backbuffer.
            // Но источник постобработки должен писать в SceneColor.
            if (!renderPass.Target.IsValid)
            {
                return renderPass.IsPostProcessSource
                    ? sceneColor
                    : backbuffer;
            }

            if (pipelineTargets.TryGetValue(
                    renderPass.Target,
                    out var existing))
            {
                return existing;
            }

            if (!_renderTargetManager.Exists(renderPass.Target))
            {
                throw new InvalidOperationException(
                    $"Render pass '{renderPass.Name}' references " +
                    $"unknown render target '{renderPass.Target.Value}'.");
            }

            var description = _renderTargetManager.GetDescription(
                renderPass.Target);

            var imported = builder.ImportRenderTarget(
                $"PipelineTarget_{pipelineTargets.Count}",
                renderPass.Target,
                description);

            pipelineTargets.Add(
                renderPass.Target,
                imported);

            return imported;
        }

        RenderGraphPassHandle? previousPass = null;

        for (var i = 0; i < _renderPipeline.Passes.Count; i++)
        {
            var renderPass = _renderPipeline.Passes[i];
            var output = ResolveTarget(renderPass);

            var passName = string.IsNullOrWhiteSpace(renderPass.Name)
                ? $"PipelinePass_{i}"
                : $"PipelinePass_{i}_{renderPass.Name}";

            var currentPass = builder.AddPass(
                passName,
                pass =>
                {
                    pass.WriteColor(
                        output,
                        renderPass.ClearColor
                            ? RenderGraphLoadAction.Clear
                            : RenderGraphLoadAction.Load);

                    pass.SetRenderState(renderPass.State);
                    pass.SetLayerRange(renderPass.Layers);
                    pass.SetContextData(renderPass.ContextData);
                    pass.SetClearDepth(renderPass.ClearColor);
                    // Сохраняем семантику старого Pipeline:
                    // каждый добавленный проход должен выполниться.
                    pass.SetSideEffect();

                    if (renderPass.IsPostProcessSource)
                    {
                        pass.SetPostProcessSource();
                    }

                    // Старый Pipeline выполнял проходы последовательно.
                    if (previousPass.HasValue)
                    {
                        pass.DependsOn(previousPass.Value);
                    }
                });

            previousPass = currentPass;

            if (!renderPass.IsPostProcessSource)
            {
                continue;
            }

            // Постобработка выполняется непосредственно после
            // прохода-источника, до следующего прохода Pipeline.
            var postProcessPass = builder.AddPass(
                $"PipelinePostProcessing_{i}",
                pass =>
                {
                    pass.Read(output);

                    pass.WriteColor(
                        backbuffer,
                        RenderGraphLoadAction.DontCare);

                    pass.SetRenderState(RenderState.Default2D);
                    pass.SetExecuteQueuedCommands(false);
                    pass.SetSideEffect();

                    pass.SetExecute(_ =>
                    {
                        if (_passExecutor is not
                            IPostProcessRenderPassExecutor postProcessExecutor)
                        {
                            throw new NotSupportedException(
                                "The render-pass executor does not support post-processing.");
                        }

                        postProcessExecutor.ExecutePostProcessing(
                            _renderPipeline.PostProcessing);
                    });

                    pass.DependsOn(currentPass);
                });

            previousPass = postProcessPass;
        }

        builder.SetOutput(backbuffer);

        return new RenderGraphCompiler().Compile(
            builder.Build());
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

    private void ValidateRenderPipeline()
    {
        var sourcePassCount = _renderPipeline.Passes.Count(
            pass => pass.IsPostProcessSource);

        var postProcessingEnabled =
            _renderPipeline.PostProcessing.HasEnabledEffects;

        if (sourcePassCount > 1 ||
            (postProcessingEnabled && sourcePassCount != 1))
        {
            throw new InvalidOperationException(
                "An enabled post-process chain requires exactly one " +
                "render pass marked as its source.");
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

    public void Submit(
    DrawWorldTextureData command)
    {
        ThrowIfDisposed();

        _renderQueue.Submit(
            command);
    }

    public TimeSpan LastEndFrameTime { get; private set; }

    public void EndFrame()
    {
        ThrowIfDisposed();

        ValidateRenderPipeline();

        if (_compiledPipelineRevision != _renderPipeline.Revision)
        {
            var updatedPlan = BuildRenderGraph(
                _logicalWidth,
                _logicalHeight);

            _renderGraphPlan = updatedPlan;
            _compiledPipelineRevision = _renderPipeline.Revision;
        }

        using var result = _renderGraphExecutor.Execute(
            _renderGraphPlan,
            _renderQueue);
    }

    public void Resize(int width, int height)
    {
        Resize(
            width,
            height,
            width,
            height);
    }

    public void Resize(
        int logicalWidth,
        int logicalHeight,
        int framebufferWidth,
        int framebufferHeight)
    {
        ThrowIfDisposed();

        _renderTargetManager.Resize(
            _sceneColorTarget,
            logicalWidth,
            logicalHeight);

        _passExecutor.Resize(
            logicalWidth,
            logicalHeight,
            framebufferWidth,
            framebufferHeight);

        _logicalWidth = logicalWidth;
        _logicalHeight = logicalHeight;

        _compiledPipelineRevision = -1;

        _renderGraphPlan = BuildRenderGraph(
            logicalWidth,
            logicalHeight);

        _compiledPipelineRevision = _renderPipeline.Revision;
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
        DisposeResource(_passExecutor, errors);
        DisposeResource(_fullscreenRenderer, errors);
        DisposeResource(_fontManager, errors);
        DisposeResource(_renderGraphExecutor, errors);
        DisposeResource(_renderTargetManager, errors);
        DisposeResource(_textureManager, errors);
        DisposeResource(_meshManager, errors);
        DisposeResource(_bufferManager, errors);
        DisposeResource(_shaderManager, errors);
        DisposeResource(_gl, errors);
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
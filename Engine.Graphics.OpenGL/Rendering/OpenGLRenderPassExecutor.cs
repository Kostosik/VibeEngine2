
using Engine.Graphics.Commands;
using Engine.Graphics.OpenGL.Resources;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace Engine.Graphics.OpenGL.Rendering;

internal sealed class OpenGLRenderPassExecutor :
    IRenderPassExecutor,
    IPostProcessRenderPassExecutor,
    IDisposable
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

    private readonly OpenGLShaderManager _shaderManager;
    private readonly OpenGLFullscreenRenderer _fullscreenRenderer;

    private RenderTargetHandle _sceneTarget;
    private RenderTargetHandle _activePostProcessSource;

    private PingPongRenderTargets? _postProcessTargets;

    private bool _disposed;

    private static readonly RenderState FullscreenRenderState = new(
        RenderBlendMode.Disabled,
        DepthTestEnabled: false,
        RenderCullMode.Disabled);

    public OpenGLRenderPassExecutor(
        GL gl,
        OpenGLRenderTargetManager renderTargets,
        OpenGLRenderCommandDispatcher dispatcher,
        OpenGLShaderManager shaderManager,
        OpenGLFullscreenRenderer fullscreenRenderer,
        int renderWidth,
        int renderHeight,
        int framebufferWidth,
        int framebufferHeight)
    {
        ArgumentNullException.ThrowIfNull(gl);
        ArgumentNullException.ThrowIfNull(renderTargets);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(shaderManager);
        ArgumentNullException.ThrowIfNull(fullscreenRenderer);

        _shaderManager = shaderManager;
        _fullscreenRenderer = fullscreenRenderer;
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
        ThrowIfDisposed();

        var targetHandle = context.Target;

        if (context.IsPostProcessSource)
        {
            if (!targetHandle.IsValid)
            {
                EnsureSceneTarget(
                    _backBufferRenderWidth,
                    _backBufferRenderHeight);

                targetHandle = _sceneTarget;
            }

            _activePostProcessSource = targetHandle;
        }

        var framebuffer = 0;

        // По умолчанию используем размеры главного окна.
        var viewportWidth = _backBufferFramebufferWidth;
        var viewportHeight = _backBufferFramebufferHeight;

        var renderWidth = _backBufferRenderWidth;
        var renderHeight = _backBufferRenderHeight;

        // Для отдельного RenderTarget размеры совпадают
        // с его собственным разрешением.
        if (targetHandle.IsValid)
        {
            framebuffer = (int)_renderTargets.GetFramebuffer(
                targetHandle);

            var description = _renderTargets.GetDescription(
                targetHandle);

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
                    ClearBufferMask.ColorBufferBit |
                    ClearBufferMask.DepthBufferBit);
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

    public void ExecutePostProcessing(PostProcessChain chain)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(chain);

        if (!_activePostProcessSource.IsValid)
        {
            throw new InvalidOperationException(
                "Post-processing source render target is not available.");
        }

        try
        {
            var sourceDescription = _renderTargets.GetDescription(
                _activePostProcessSource);

            // Источник итогового изображения.
            // При отсутствии эффектов это World RenderTarget.
            var sourceTarget = _activePostProcessSource;

            if (chain.HasEnabledEffects)
            {
                EnsurePostProcessTargets(sourceDescription);

                var sourceTexture = _renderTargets.GetColorTexture(
                    sourceTarget);

                _dispatcher.ApplyState(FullscreenRenderState);

                foreach (var effect in chain.Effects)
                {
                    if (!effect.Enabled)
                        continue;

                    if (!effect.Shader.IsValid ||
                        !_shaderManager.Exists(effect.Shader))
                    {
                        throw new InvalidOperationException(
                            $"Post-process effect '{effect.Name}' " +
                            "references an invalid shader.");
                    }

                    var writeTarget = _postProcessTargets!.WriteTarget;

                    _gl.BindFramebuffer(
                        FramebufferTarget.Framebuffer,
                        _renderTargets.GetFramebuffer(writeTarget));

                    _gl.Viewport(
                        0,
                        0,
                        (uint)sourceDescription.Width,
                        (uint)sourceDescription.Height);

                    _fullscreenRenderer.Draw(
                        _shaderManager.GetProgram(effect.Shader),
                        sourceTexture,
                        effect,
                        sourceDescription.Width,
                        sourceDescription.Height);

                    _postProcessTargets.Swap();

                    sourceTarget = _postProcessTargets.ReadTarget;
                    sourceTexture = _postProcessTargets.ReadTexture;
                }
            }

            // Получаем framebuffer итогового изображения.
            var sourceFramebuffer = _renderTargets.GetFramebuffer(
                sourceTarget);

            // Читаем изображение из RenderTarget,
            // а выводим непосредственно в framebuffer окна.
            _gl.BindFramebuffer(
                FramebufferTarget.ReadFramebuffer,
                sourceFramebuffer);

            _gl.BindFramebuffer(
                FramebufferTarget.DrawFramebuffer,
                0);

            // Сохраняем вертикальное отражение,
            // которое ранее выполнялось в CopyFragmentShader.
            _gl.BlitFramebuffer(
                0,
                sourceDescription.Height,
                sourceDescription.Width,
                0,
                0,
                0,
                _backBufferFramebufferWidth,
                _backBufferFramebufferHeight,
                ClearBufferMask.ColorBufferBit,
                BlitFramebufferFilter.Nearest);
        }
        finally
        {
            _activePostProcessSource = RenderTargetHandle.Invalid;

            try
            {
                _gl.BindFramebuffer(
                    FramebufferTarget.Framebuffer,
                    0);
            }
            finally
            {
                _gl.Viewport(
                    0,
                    0,
                    (uint)_backBufferFramebufferWidth,
                    (uint)_backBufferFramebufferHeight);
            }
        }
    }

    private void EnsureSceneTarget(int width, int height)
    {
        if (!_sceneTarget.IsValid)
        {
            _sceneTarget = _renderTargets.Create(
                new RenderTargetDescription(width, height));

            return;
        }

        var description = _renderTargets.GetDescription(
            _sceneTarget);

        if (description.Width != width ||
            description.Height != height)
        {
            _renderTargets.Resize(
                _sceneTarget,
                width,
                height);
        }
    }

    private void EnsurePostProcessTargets(
    RenderTargetDescription sourceDescription)
    {
        if (_postProcessTargets is not null)
        {
            var current = _postProcessTargets.Description;

            if (current.Format != sourceDescription.Format)
            {
                _postProcessTargets.Dispose();
                _postProcessTargets = null;
            }
        }

        if (_postProcessTargets is null)
        {
            _postProcessTargets = new PingPongRenderTargets(
                _renderTargets,
                sourceDescription);

            return;
        }

        var currentDescription = _postProcessTargets.Description;

        if (currentDescription.Width != sourceDescription.Width ||
            currentDescription.Height != sourceDescription.Height)
        {
            _postProcessTargets.Resize(
                sourceDescription.Width,
                sourceDescription.Height);
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

        // Сначала обновляем существующую цель.
        // Если обновление завершится ошибкой, старые размеры
        // backbuffer останутся действующими.
        if (_sceneTarget.IsValid)
        {
            _renderTargets.Resize(
                _sceneTarget,
                renderWidth,
                renderHeight);
        }

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

    public void Dispose()
    {
        if (_disposed)
            return;

        var errors = new List<Exception>();

        try
        {
            _postProcessTargets?.Dispose();
        }
        catch (Exception exception)
        {
            errors.Add(exception);
        }

        _postProcessTargets = null;

        if (_sceneTarget.IsValid)
        {
            try
            {
                _renderTargets.Destroy(_sceneTarget);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        _sceneTarget = RenderTargetHandle.Invalid;
        _activePostProcessSource = RenderTargetHandle.Invalid;

        _disposed = true;

        if (errors.Count > 0)
        {
            throw new AggregateException(
                "Post-processing render targets could not be fully released.",
                errors);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}

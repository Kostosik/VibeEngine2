using Engine.Graphics.Commands;
using Engine.Graphics2D.Commands;
using Engine.Graphics.OpenGL.Fonts;
using Engine.Graphics.OpenGL.Resources;
using Engine.Graphics.Rendering;

namespace Engine.Graphics.OpenGL.Rendering;

internal sealed class OpenGLRenderCommandDispatcher
{
    private enum RendererKind
    {
        None,
        Texture,
        Debug,
        Font
    }
    private readonly OpenGLRenderTargetManager _renderTargetManager;
    private readonly OpenGLTextureManager _textureManager;
    private readonly OpenGLTextureRenderer _textureRenderer;
    private readonly OpenGLDebugRenderer _debugRenderer;
    private readonly OpenGLFontRenderer _fontRenderer;
    private readonly OpenGLRenderState _renderState;
    private RendererKind _activeRenderer;
    private readonly OpenGLShaderManager _shaderManager;
    private readonly OpenGLFullscreenRenderer _fullscreenRenderer;
    public OpenGLRenderCommandDispatcher(
        OpenGLTextureManager textureManager,
        OpenGLTextureRenderer textureRenderer,
        OpenGLDebugRenderer debugRenderer,
        OpenGLFontRenderer fontRenderer,
        OpenGLRenderState renderState,
        OpenGLRenderTargetManager renderTargetManager, OpenGLShaderManager shaderManager,
        OpenGLFullscreenRenderer fullscreenRenderer)
    {
        ArgumentNullException.ThrowIfNull(textureManager);

        ArgumentNullException.ThrowIfNull(textureRenderer);

        ArgumentNullException.ThrowIfNull(debugRenderer);

        ArgumentNullException.ThrowIfNull(fontRenderer);

        ArgumentNullException.ThrowIfNull(renderState);
        ArgumentNullException.ThrowIfNull(
    renderTargetManager);
        ArgumentNullException.ThrowIfNull(shaderManager);
        ArgumentNullException.ThrowIfNull(fullscreenRenderer);
        _renderState = renderState;

        _textureManager = textureManager;

        _textureRenderer = textureRenderer;

        _debugRenderer =
            debugRenderer;

        _fontRenderer =
            fontRenderer;
        _renderTargetManager = renderTargetManager;
        _shaderManager = shaderManager;
        _fullscreenRenderer = fullscreenRenderer;
    }

    public void SetRenderSize(
    int width,
    int height)
    {
        _textureRenderer.Resize(
            width,
            height);

        _debugRenderer.Resize(
            width,
            height);

        _fontRenderer.Resize(
            width,
            height);
    }

    public void BeginFrame()
    {
        // Рендерер будет запущен только при первой
        // команде соответствующего типа.
        _activeRenderer = RendererKind.None;
    }

    public void Execute(IRenderCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        switch (command)
        {
            case DrawTextureCommand drawTexture:

                if (!_textureManager.Exists(drawTexture.Texture))
                    return;

                BeginTextureRenderer();

                _textureRenderer.Draw(drawTexture);
                return;

            case DrawRenderTargetCommand drawRenderTarget:

                if (!_renderTargetManager.Exists(drawRenderTarget.Target))
                    return;

                var renderTargetTexture =
                    _renderTargetManager.GetColorTexture(
                        drawRenderTarget.Target);

                if (!_textureManager.Exists(renderTargetTexture))
                    return;

                BeginTextureRenderer();

                _textureRenderer.Draw(
                    new DrawTextureCommand(
                        renderTargetTexture,
                        drawRenderTarget.Position,
                        drawRenderTarget.Size,
                        drawRenderTarget.UV,
                        drawRenderTarget.Layer));

                return;

            case DrawUiTextureCommand drawUiTexture:

                if (!_textureManager.Exists(drawUiTexture.Texture))
                    return;

                BeginTextureRenderer();

                _textureRenderer.Draw(drawUiTexture);
                return;

            case DrawUiTextCommand drawUiText:

                BeginFontRenderer();

                _fontRenderer.Draw(drawUiText);
                return;

            case DrawUiRectangleCommand drawUiRectangle:

                BeginDebugRenderer();

                _debugRenderer.Draw(drawUiRectangle);
                return;

            case DrawDebugTextCommand drawDebugText:

                BeginDebugRenderer();

                _debugRenderer.Draw(drawDebugText);
                return;

            case DrawWorldTextureCommand drawWorldTexture:

                if (!_textureManager.Exists(drawWorldTexture.Texture))
                    return;

                BeginTextureRenderer();

                _textureRenderer.DrawWorld(drawWorldTexture);
                return;

            case DrawDebugLineCommand debugLine:

                BeginDebugRenderer();

                _debugRenderer.Draw(debugLine);
                return;

            case DrawDebugRectangleCommand debugRectangle:

                BeginDebugRenderer();

                _debugRenderer.Draw(debugRectangle);
                return;

            case DrawDebugCircleCommand debugCircle:

                BeginDebugRenderer();

                _debugRenderer.Draw(debugCircle);
                return;
            case DrawFullscreenCommand fullscreen:

                if (!_textureManager.Exists(fullscreen.Source) ||
                    !_shaderManager.Exists(fullscreen.Shader))
                {
                    return;
                }

                // Сначала завершаем накопленный batch спрайтов,
                // затем выполняем полноэкранный draw.
                EndActiveRenderer();

                _fullscreenRenderer.Draw(
                    _shaderManager.GetProgram(fullscreen.Shader),
                    fullscreen.Source);

                return;

            default:
                throw new NotSupportedException(
                    $"Render command '{command.GetType().Name}' " +
                    "is not supported.");
        }
    }

    public void Execute(DrawWorldTextureData command)
    {
        if (!_textureManager.Exists(command.Texture))
            return;

        BeginTextureRenderer();

        _textureRenderer.DrawWorld(command);
    }

    public void EndFrame()
    {
        EndActiveRenderer();
    }



    public void Resize(
        int width,
        int height)
    {
        _textureRenderer.Resize(
            width,
            height);

        _debugRenderer.Resize(
            width,
            height);

        _fontRenderer.Resize(
            width,
            height);
    }

    private void BeginTextureRenderer()
    {
        if (_activeRenderer ==
            RendererKind.Texture)
        {
            return;
        }

        EndActiveRenderer();

        _textureRenderer.Begin();

        _activeRenderer =
            RendererKind.Texture;
    }

    private void BeginDebugRenderer()
    {
        if (_activeRenderer ==
            RendererKind.Debug)
        {
            return;
        }

        EndActiveRenderer();

        _debugRenderer.Begin();

        _activeRenderer =
            RendererKind.Debug;
    }

    private void BeginFontRenderer()
    {
        if (_activeRenderer ==
            RendererKind.Font)
        {
            return;
        }

        EndActiveRenderer();

        _fontRenderer.Begin();

        _activeRenderer =
            RendererKind.Font;
    }

    private void EndActiveRenderer()
    {
        switch (_activeRenderer)
        {
            case RendererKind.Texture:
                _textureRenderer.End();
                break;

            case RendererKind.Debug:
                _debugRenderer.End();
                break;

            case RendererKind.Font:
                _fontRenderer.End();
                break;
        }

        _activeRenderer =
            RendererKind.None;
    }

    public void ApplyState(
    RenderState state)
    {
        _renderState.Apply(state);
    }
}
using Engine.Graphics.Commands;
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

    private readonly OpenGLTextureManager _textureManager;
    private readonly OpenGLTextureRenderer _textureRenderer;
    private readonly OpenGLDebugRenderer _debugRenderer;
    private readonly OpenGLFontRenderer _fontRenderer;
    private readonly OpenGLRenderState _renderState;
    private RendererKind _activeRenderer;

    public OpenGLRenderCommandDispatcher(
        OpenGLTextureManager textureManager,
        OpenGLTextureRenderer textureRenderer,
        OpenGLDebugRenderer debugRenderer,
        OpenGLFontRenderer fontRenderer,OpenGLRenderState renderState)
    {
        ArgumentNullException.ThrowIfNull(textureManager);

        ArgumentNullException.ThrowIfNull(textureRenderer);

        ArgumentNullException.ThrowIfNull(debugRenderer);

        ArgumentNullException.ThrowIfNull(fontRenderer);

        ArgumentNullException.ThrowIfNull(renderState);

        _renderState = renderState;

        _textureManager = textureManager;

        _textureRenderer = textureRenderer;

        _debugRenderer =
            debugRenderer;

        _fontRenderer =
            fontRenderer;
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
        _activeRenderer =
            RendererKind.Texture;

        _textureRenderer.Begin();
    }

    public void Execute(
        IRenderCommand command)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        switch (command)
        {
            case DrawTextureCommand drawTexture:

                BeginTextureRenderer();

                if (!_textureManager.Exists(
                        drawTexture.Texture))
                {
                    return;
                }

                _textureRenderer.Draw(
                    drawTexture);

                return;

            case DrawUiTextureCommand drawUiTexture:

                BeginTextureRenderer();

                if (!_textureManager.Exists(
                        drawUiTexture.Texture))
                {
                    return;
                }

                _textureRenderer.Draw(
                    drawUiTexture);

                return;

            case DrawUiTextCommand drawUiText:

                BeginFontRenderer();

                _fontRenderer.Draw(
                    drawUiText);

                return;

            case DrawUiRectangleCommand drawUiRectangle:

                BeginDebugRenderer();

                _debugRenderer.Draw(
                    drawUiRectangle);

                return;

            case DrawDebugTextCommand drawDebugText:

                BeginDebugRenderer();

                _debugRenderer.Draw(
                    drawDebugText);

                return;

            case DrawWorldTextureCommand drawWorldTexture:

                BeginTextureRenderer();

                if (!_textureManager.Exists(
                        drawWorldTexture.Texture))
                {
                    return;
                }

                _textureRenderer.DrawWorld(
                    drawWorldTexture);

                return;

            case DrawDebugLineCommand debugLine:

                BeginDebugRenderer();

                _debugRenderer.Draw(
                    debugLine);

                return;

            case DrawDebugRectangleCommand debugRectangle:

                BeginDebugRenderer();

                _debugRenderer.Draw(
                    debugRectangle);

                return;

            case DrawDebugCircleCommand debugCircle:

                BeginDebugRenderer();

                _debugRenderer.Draw(
                    debugCircle);

                return;

            default:
                throw new NotSupportedException(
                    $"Render command '{command.GetType().Name}' " +
                    "is not supported.");
        }
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
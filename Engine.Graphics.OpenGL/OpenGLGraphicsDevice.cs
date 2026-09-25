using Engine.Graphics.Cameras;
using Engine.Graphics.Commands;
using Engine.Graphics.Fonts;
using Engine.Graphics.OpenGL.Fonts;
using Engine.Graphics.OpenGL.Rendering;
using Engine.Graphics.OpenGL.Resources;
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
    public IFontManager Fonts =>
    _fontManager;
    private readonly List<QueuedCommand> _commands = new();
    private readonly record struct QueuedCommand(
    IRenderCommand Command,
    int Layer,
    long Order);

    private long _commandOrder;

    private enum RendererKind
    {
        None,
        Texture,
        Debug,
        Font
    }

    private RendererKind _activeRenderer;
    public ITextureManager Textures =>
        _textureManager;

    public OpenGLGraphicsDevice(
        GL gl,
        int width,
        int height,
        Camera camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        _gl = gl;

        _textureManager =
            new OpenGLTextureManager(gl);

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

        Configure();
    }

    public void BeginFrame()
    {
        _gl.Clear(
            ClearBufferMask.ColorBufferBit);

        _commands.Clear();

        _commandOrder = 0;

        _activeRenderer =
            RendererKind.Texture;

        _textureRenderer.Begin();
    }

    public void Submit(
        IRenderCommand command)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        _commands.Add(
            new QueuedCommand(
                command,
                command.Layer,
                _commandOrder++));
    }

    public void EndFrame()
    {
        _commands.Sort(
            static (left, right) =>
            {
                var layerComparison =
                    left.Layer.CompareTo(
                        right.Layer);

                if (layerComparison != 0)
                {
                    return layerComparison;
                }

                return left.Order.CompareTo(
                    right.Order);
            });

        foreach (var queued in _commands)
        {
            switch (queued.Command)
            {
                case DrawTextureCommand drawTexture:

                    BeginTextureRenderer();

                    if (!_textureManager.Exists(
                            drawTexture.Texture))
                    {
                        continue;
                    }

                    _textureRenderer.Draw(
                        drawTexture);

                    break;

                case DrawUiTextureCommand drawUiTexture:

                    BeginTextureRenderer();

                    if (!_textureManager.Exists(
                            drawUiTexture.Texture))
                    {
                        continue;
                    }

                    _textureRenderer.Draw(
                        drawUiTexture);

                    break;

                case DrawUiTextCommand drawUiText:

                    BeginFontRenderer();

                    _fontRenderer.Draw(
                        drawUiText);

                    break;

                case DrawUiRectangleCommand drawUiRectangle:

                    BeginDebugRenderer();

                    _debugRenderer.Draw(
                        drawUiRectangle);

                    break;

                case DrawDebugTextCommand drawDebugText:

                    BeginDebugRenderer();

                    _debugRenderer.Draw(
                        drawDebugText);

                    break;

                case DrawWorldTextureCommand drawWorldTexture:

                    BeginTextureRenderer();

                    if (!_textureManager.Exists(
                            drawWorldTexture.Texture))
                    {
                        continue;
                    }

                    _textureRenderer.DrawWorld(
                        drawWorldTexture);

                    break;

                case DrawDebugLineCommand debugLine:

                    BeginDebugRenderer();

                    _debugRenderer.Draw(
                        debugLine);

                    break;

                case DrawDebugRectangleCommand debugRectangle:

                    BeginDebugRenderer();

                    _debugRenderer.Draw(
                        debugRectangle);

                    break;

                case DrawDebugCircleCommand debugCircle:

                    BeginDebugRenderer();

                    _debugRenderer.Draw(
                        debugCircle);

                    break;

                default:
                    throw new NotSupportedException(
                        $"Render command '{queued.Command.GetType().Name}' " +
                        "is not supported.");
            }
        }

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

    public void Dispose()
    {
        _debugRenderer.Dispose();
        _textureRenderer.Dispose();
        _textureManager.Dispose();
        _fontManager.Dispose();
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

    private void Configure()
    {
        _gl.ClearColor(
            0.05f,
            0.05f,
            0.05f,
            1.0f);

        _gl.Disable(
            EnableCap.DepthTest);

        _gl.Enable(
            EnableCap.Blend);

        _gl.BlendFunc(
            BlendingFactor.SrcAlpha,
            BlendingFactor.OneMinusSrcAlpha);
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
}
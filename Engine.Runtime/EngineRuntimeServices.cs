using Engine.Audio;
using Engine.Content;
using Engine.Graphics;
using Engine.Camera;
using Engine.Input;

namespace Engine.Runtime;

public sealed class EngineRuntimeServices
{
    public EngineRuntimeServices(
        IGraphicsDevice graphics,
        IInput input,
        Camera2D camera,
        IAudioManager audio,
        IContentManager content)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(content);

        _graphics = graphics;
        _input = input;
        _camera = camera;
        _audio = audio;
        _content = content;
    }

    public IGraphicsDevice Graphics =>
        _graphics
        ?? throw new InvalidOperationException(
            "Graphics service is not configured.");

    public IInput Input =>
        _input
        ?? throw new InvalidOperationException(
            "Input service is not configured.");

    public Camera2D Camera =>
        _camera
        ?? throw new InvalidOperationException(
            "Camera service is not configured.");

    public IAudioManager Audio =>
        _audio
        ?? throw new InvalidOperationException(
            "Audio service is not configured.");

    public IContentManager Content =>
        _content
        ?? throw new InvalidOperationException(
            "Content service is not configured.");

    private readonly IGraphicsDevice? _graphics;
    private readonly IInput? _input;
    private readonly Camera2D? _camera;
    private readonly IAudioManager? _audio;
    private readonly IContentManager? _content;

    public EngineRuntimeServices()
    {
    }
}
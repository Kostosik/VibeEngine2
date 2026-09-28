using Engine.Audio;
using Engine.Content;
using Engine.Graphics;
using Engine.Graphics.Cameras;
using Engine.Input;

namespace Engine.Runtime;

public sealed class EngineRuntimeServices
{
    public EngineRuntimeServices(
        IGraphicsDevice graphics,
        IInput input,
        Camera camera,
        IAudioManager audio,
        IContentManager content)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(content);

        Graphics = graphics;
        Input = input;
        Camera = camera;
        Audio = audio;
        Content = content;
    }

    public IGraphicsDevice Graphics { get; }

    public IInput Input { get; }

    public Camera Camera { get; }

    public IAudioManager Audio { get; }

    public IContentManager Content { get; }
}
using Engine.Camera;
using Engine.Core.Application;
using Engine.Graphics;
using Engine.Input;

namespace Engine.Hosting;

/// <summary>
/// Platform host responsible for the game window,
/// frame execution and platform lifecycle.
/// </summary>
public interface IGameHost : IDisposable
{
    IGraphicsDevice Graphics { get; }

    IInputBackend InputBackend { get; }

    Camera2D Camera { get; }

    void Run(GameLoop gameLoop);
}
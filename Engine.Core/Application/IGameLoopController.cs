namespace Engine.Core.Application;

public interface IGameLoopController
{
    bool IsPaused { get; }

    void Pause();

    void Resume();

    bool RequestStep();
}
namespace Engine.Core.Application;

public interface IShutdownGuard
{
    bool CanShutdown { get; }

    void RequestShutdown();
}
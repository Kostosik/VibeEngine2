using Engine.Core.Application;

namespace Game.Sandbox;

public sealed class SandboxApplication :
    StatefulApplication,
    IDisposable
{
    private readonly IDisposable _ownedResources;
    private bool _disposed;

    public SandboxApplication(
        IApplicationState initialState,
        IDisposable ownedResources)
        : base(initialState)
    {
        ArgumentNullException.ThrowIfNull(ownedResources);

        _ownedResources = ownedResources;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _ownedResources.Dispose();
    }
}
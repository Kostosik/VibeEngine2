using Engine.Core.Application;

namespace Engine.Tooling.Debugging;

public sealed class DebugExecutionController
{
    private readonly IGameLoopController _loop;

    public DebugExecutionController(
        IGameLoopController loop)
    {
        ArgumentNullException.ThrowIfNull(
            loop);

        _loop = loop;
    }

    public bool IsPaused =>
        _loop.IsPaused;

    public DebugCommandResult Pause()
    {
        if (_loop.IsPaused)
        {
            return DebugCommandResult.Ok(
                "Game is already paused.");
        }

        _loop.Pause();

        return DebugCommandResult.Ok(
            "Game paused.");
    }

    public DebugCommandResult Resume()
    {
        if (!_loop.IsPaused)
        {
            return DebugCommandResult.Ok(
                "Game is already running.");
        }

        _loop.Resume();

        return DebugCommandResult.Ok(
            "Game resumed.");
    }

    public DebugCommandResult Step()
    {
        if (!_loop.IsPaused)
        {
            return DebugCommandResult.Fail(
                "Game must be paused before stepping.");
        }

        if (!_loop.RequestStep())
        {
            return DebugCommandResult.Fail(
                "Simulation step could not be requested.");
        }

        return DebugCommandResult.Ok(
            "Simulation step requested.");
    }
}
using Engine.Editor.Projects;
using Engine.Tooling.Debugging;

namespace Engine.Editor.UI.Commands;

public sealed class GameProjectSessionCommand : IDebugCommand
{
    private readonly Func<GameProject?> _getProject;
    private readonly GameProjectSessionManager _manager;
    private readonly Func<bool> _isEngineRebuildRunning;

    public GameProjectSessionCommand(
        Func<GameProject?> getProject,
        GameProjectSessionManager manager,
        Func<bool> isEngineRebuildRunning)
    {
        ArgumentNullException.ThrowIfNull(getProject);
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(isEngineRebuildRunning);

        _getProject = getProject;
        _manager = manager;
        _isEngineRebuildRunning = isEngineRebuildRunning;
    }

    public string Name => "game-session";

    public string Description =>
        "Starts and controls multiple game project sessions.";

    public DebugCommandResult Execute(
        IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (arguments.Count == 0)
        {
            return Usage();
        }

        var operation = arguments[0].ToLowerInvariant();

        if (operation == "list")
        {
            return ListSessions(arguments);
        }

        if (operation == "stop-all")
        {
            if (arguments.Count != 1)
            {
                return DebugCommandResult.Fail(
                    "Usage: game-session stop-all");
            }

            _manager.StopAll();

            return DebugCommandResult.Ok(
                "All managed game sessions stopped.");
        }

        if (operation == "start")
        {
            return StartSession(arguments);
        }

        if (operation is "restart" or "stop" or "remove")
        {
            return ControlSession(operation, arguments);
        }

        return Usage();
    }

    private DebugCommandResult StartSession(
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count < 2)
        {
            return DebugCommandResult.Fail(
                "Usage: game-session start <name> [application arguments...]");
        }

        if (_isEngineRebuildRunning())
        {
            return DebugCommandResult.Fail(
                "Cannot start a session while rebuilding engine DLLs.");
        }

        var project = _getProject();

        if (project is null)
        {
            return DebugCommandResult.Fail(
                "Open a game project first.");
        }

        var name = arguments[1];
        var applicationArguments = arguments.Skip(2).ToArray();

        try
        {
            var id = _manager.Start(
                project,
                name,
                GameProjectRunMode.Standard,
                applicationArguments);

            return DebugCommandResult.Ok(
                $"Started game session '{name}' ({id:N}).");
        }
        catch (Exception exception)
        {
            return DebugCommandResult.Fail(exception.Message);
        }
    }

    private DebugCommandResult ControlSession(
        string operation,
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 2)
        {
            return DebugCommandResult.Fail(
                $"Usage: game-session {operation} <name>");
        }

        if (operation == "restart" &&
            _isEngineRebuildRunning())
        {
            return DebugCommandResult.Fail(
                "Cannot restart a session while rebuilding engine DLLs.");
        }

        var name = arguments[1];

        var session = _manager.Sessions.FirstOrDefault(
            item => string.Equals(
                item.Name,
                name,
                StringComparison.OrdinalIgnoreCase));

        if (session is null)
        {
            return DebugCommandResult.Fail(
                $"Game session '{name}' was not found.");
        }

        try
        {
            switch (operation)
            {
                case "stop":
                    _manager.Stop(session.Id);
                    break;

                case "restart":
                    _manager.Restart(session.Id);
                    break;

                case "remove":
                    _manager.Remove(session.Id);
                    break;

                default:
                    return Usage();
            }

            return DebugCommandResult.Ok(
                $"Session '{name}': {operation} completed.");
        }
        catch (Exception exception)
        {
            return DebugCommandResult.Fail(exception.Message);
        }
    }

    private DebugCommandResult ListSessions(
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 1)
        {
            return DebugCommandResult.Fail(
                "Usage: game-session list");
        }

        var sessions = _manager.Sessions;

        if (sessions.Count == 0)
        {
            return DebugCommandResult.Ok(
                "No managed game sessions.");
        }

        return DebugCommandResult.Ok(
            string.Join(
                Environment.NewLine,
                sessions.Select(session =>
                    $"{session.Name}: " +
                    $"{(session.IsActive ? "Running" : "Stopped")} | " +
                    $"{session.ProjectName} | " +
                    $"{session.RunMode} | " +
                    $"Args: {string.Join(" ", session.ApplicationArguments)}")));
    }

    private static DebugCommandResult Usage()
    {
        return DebugCommandResult.Ok(
            """
            Game session commands:
              game-session list
              game-session start <name> [application arguments...]
              game-session stop <name>
              game-session restart <name>
              game-session remove <name>
              game-session stop-all
            """);
    }
}
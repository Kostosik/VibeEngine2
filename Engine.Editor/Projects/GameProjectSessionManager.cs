using System.Collections.Concurrent;

namespace Engine.Editor.Projects;

public sealed record GameProjectSessionOutput(
    Guid SessionId,
    string SessionName,
    bool IsError,
    string Message);

public sealed record GameProjectSessionStatus(
    Guid Id,
    string Name,
    string ProjectName,
    GameProjectRunMode RunMode,
    bool IsActive,
    IReadOnlyList<string> ApplicationArguments);

/// <summary>
/// Manages multiple independently controlled game processes.
/// </summary>
public sealed class GameProjectSessionManager : IDisposable
{
    private sealed class ManagedSession
    {
        public ManagedSession(
            Guid id,
            string name,
            GameProject project,
            GameProjectRunMode runMode,
            string[] applicationArguments,
            GameProjectRunner runner)
        {
            Id = id;
            Name = name;
            Project = project;
            RunMode = runMode;
            ApplicationArguments = applicationArguments;
            Runner = runner;
        }

        public Guid Id { get; }

        public string Name { get; }

        public GameProject Project { get; }

        public GameProjectRunMode RunMode { get; }

        public string[] ApplicationArguments { get; }

        public GameProjectRunner Runner { get; }
    }

    private readonly Dictionary<Guid, ManagedSession> _sessions = new();

    private readonly ConcurrentQueue<GameProjectSessionOutput> _output = new();

    private bool _disposed;

    public IReadOnlyList<GameProjectSessionStatus> Sessions =>
        _sessions.Values
            .Select(CreateStatus)
            .ToArray();

    public Guid Start(
        GameProject project,
        string name,
        GameProjectRunMode mode = GameProjectRunMode.Standard,
        IReadOnlyList<string>? applicationArguments = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (_sessions.Values.Any(session =>
        string.Equals(
            session.Name,
            name,
            StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"A game session named '{name}' already exists.");
        }

        var arguments =
            applicationArguments?.ToArray()
            ?? Array.Empty<string>();

        var runner = new GameProjectRunner();

        try
        {
            runner.Start(project, mode, arguments);

            var id = Guid.NewGuid();

            _sessions.Add(
                id,
                new ManagedSession(
                    id,
                    name,
                    project,
                    mode,
                    arguments,
                    runner));

            return id;
        }
        catch
        {
            runner.Dispose();
            throw;
        }
    }

    public void Stop(Guid sessionId)
    {
        var session = GetSession(sessionId);

        session.Runner.Stop();

        CollectOutput(session);
    }

    public void Restart(Guid sessionId)
    {
        var session = GetSession(sessionId);

        if (session.Runner.IsSessionActive)
        {
            session.Runner.Stop();
        }

        CollectOutput(session);

        session.Runner.Start(
            session.Project,
            session.RunMode,
            session.ApplicationArguments);
    }

    public bool Remove(Guid sessionId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return false;
        }

        try
        {
            session.Runner.Stop();
        }
        finally
        {
            CollectOutput(session);
            session.Runner.Dispose();
            _sessions.Remove(sessionId);
        }

        return true;
    }

    public void StopAll()
    {
        foreach (var session in _sessions.Values.ToArray())
        {
            Stop(session.Id);
        }
    }

    public bool TryDequeueOutput(
        out GameProjectSessionOutput? output)
    {
        CollectAllOutput();

        return _output.TryDequeue(out output);
    }

    private ManagedSession GetSession(Guid sessionId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            throw new KeyNotFoundException(
                $"Game session '{sessionId}' was not found.");
        }

        return session;
    }

    private static GameProjectSessionStatus CreateStatus(
        ManagedSession session)
    {
        return new GameProjectSessionStatus(
            session.Id,
            session.Name,
            session.Project.Name,
            session.RunMode,
            session.Runner.IsSessionActive,
            Array.AsReadOnly(session.ApplicationArguments));
    }

    private void CollectAllOutput()
    {
        foreach (var session in _sessions.Values)
        {
            CollectOutput(session);
        }
    }

    private void CollectOutput(ManagedSession session)
    {
        while (session.Runner.TryDequeueOutput(out var output))
        {
            if (output is null)
            {
                continue;
            }

            _output.Enqueue(
                new GameProjectSessionOutput(
                    session.Id,
                    session.Name,
                    output.IsError,
                    output.Message));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var session in _sessions.Values.ToArray())
        {
            try
            {
                session.Runner.Stop();
            }
            catch
            {
                // Continue disposing the remaining sessions.
            }
            finally
            {
                CollectOutput(session);
                session.Runner.Dispose();
            }
        }

        _sessions.Clear();
    }
}
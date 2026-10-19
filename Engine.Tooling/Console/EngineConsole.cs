using Engine.Core.Diagnostics;
using Engine.Tooling.Debugging;
using System.Text;

namespace Engine.Tooling.Console;

public sealed class EngineConsole : IDisposable
{
    private readonly DebugCommandRegistry? _registry;
    private readonly ConsoleHistory _history;

    private readonly DebugConsole? _forwardConsole;
    private readonly Action<ConsoleMessage>? _forwardHandler;

    private readonly List<ConsoleMessage> _messages = new();

    public EngineConsole(
        DebugCommandRegistry registry,
        ConsoleHistory? history = null)
    {
        ArgumentNullException.ThrowIfNull(
            registry);

        _registry = registry;

        _history =
            history ??
            new ConsoleHistory();

        _forwardConsole = null;
        _forwardHandler = null;
    }

    public EngineConsole(DebugConsole console)
    {
        ArgumentNullException.ThrowIfNull(console);

        _registry = null;
        _history = console.History;
        _forwardConsole = console;

        _forwardHandler = message =>
            MessageWritten?.Invoke(message);

        console.MessageWritten += _forwardHandler;
    }

    public IReadOnlyList<ConsoleMessage> Messages =>
        _forwardConsole?.Messages ?? _messages;

    public ConsoleHistory History =>
        _forwardConsole?.History ?? _history;

    public event Action<ConsoleMessage>? MessageWritten;

    public DebugCommandResult Execute(
        string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            input);

        if (_forwardConsole is not null)
        {
            return _forwardConsole.Execute(input);
        }

        _history.Add(
            input);

        IReadOnlyList<string> tokens;

        try
        {
            tokens =
                Tokenize(input);
        }
        catch (FormatException exception)
        {
            var result =
                DebugCommandResult.Fail(
                    exception.Message);

            Write(
                DiagnosticLevel.Error,
                result.Message);

            return result;
        }

        if (tokens.Count == 0)
        {
            return DebugCommandResult.Ok(
                string.Empty);
        }

        var commandName =
            tokens[0];

        if (!_registry.TryGet(
                commandName,
                out var command))
        {
            var result =
                DebugCommandResult.Fail(
                    $"Unknown command '{commandName}'.");

            Write(
                DiagnosticLevel.Error,
                result.Message);

            return result;
        }

        var arguments =
            tokens
                .Skip(1)
                .ToArray();

        try
        {
            var result =
                command!.Execute(
                    arguments);

            if (!string.IsNullOrWhiteSpace(
                    result.Message))
            {
                Write(
                    result.Success
                        ? DiagnosticLevel.Info
                        : DiagnosticLevel.Error,
                    result.Message);
            }

            return result;
        }
        catch (Exception exception)
        {
            var result =
                DebugCommandResult.Fail(
                    $"Command '{commandName}' failed: " +
                    exception.Message);

            Write(
                DiagnosticLevel.Error,
                result.Message);

            return result;
        }
    }

    public void Write(
     DiagnosticLevel level,
     string message,
     string? code = null,
     Exception? exception = null)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (_forwardConsole is not null)
        {
            _forwardConsole.Write(
                level,
                message,
                code,
                exception);

            return;
        }

        var consoleMessage = new ConsoleMessage(
            DateTime.UtcNow,
            level,
            message,
            code,
            exception);

        _messages.Add(consoleMessage);
        MessageWritten?.Invoke(consoleMessage);
    }

    public void WriteLine(string message)
    {
        Write(DiagnosticLevel.Info, message);
    }

    public void WriteWarning(
        string message,
        string? code = null)
    {
        Write(DiagnosticLevel.Warning, message, code);
    }

    public void WriteError(
        string message,
        string? code = null,
        Exception? exception = null)
    {
        Write(
            DiagnosticLevel.Error,
            message,
            code,
            exception);
    }

    public void WriteException(
        Exception exception,
        string? message = null,
        string? code = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Write(
            DiagnosticLevel.Error,
            string.IsNullOrWhiteSpace(message)
                ? exception.Message
                : message,
            code,
            exception);
    }

    public void Clear()
    {
        if (_forwardConsole is not null)
        {
            _forwardConsole.Clear();
            return;
        }

        _messages.Clear();
    }

    private static IReadOnlyList<string> Tokenize(
        string input)
    {
        var tokens =
            new List<string>();

        var current =
            new StringBuilder();

        var quoted =
            false;

        for (var i = 0;
             i < input.Length;
             i++)
        {
            var character =
                input[i];

            if (character == '"')
            {
                quoted = !quoted;
                continue;
            }

            if (char.IsWhiteSpace(character) &&
                !quoted)
            {
                if (current.Length > 0)
                {
                    tokens.Add(
                        current.ToString());

                    current.Clear();
                }

                continue;
            }

            current.Append(
                character);
        }

        if (quoted)
        {
            throw new FormatException(
                "Unclosed quoted argument.");
        }

        if (current.Length > 0)
        {
            tokens.Add(
                current.ToString());
        }

        return tokens;
    }

    public void Dispose()
    {
        if (_forwardConsole is not null &&
            _forwardHandler is not null)
        {
            _forwardConsole.MessageWritten -= _forwardHandler;
        }
    }
}
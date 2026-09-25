using System.Text;
using Engine.Core.Diagnostics;
using Engine.Tooling.Debugging;

namespace Engine.Tooling.Console;

public sealed class EngineConsole
{
    private readonly DebugCommandRegistry _registry;
    private readonly ConsoleHistory _history;

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
    }

    public IReadOnlyList<ConsoleMessage> Messages =>
        _messages;

    public ConsoleHistory History =>
        _history;

    public event Action<ConsoleMessage>? MessageWritten;

    public DebugCommandResult Execute(
        string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            input);

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
        string? code = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            message);

        var consoleMessage =
            new ConsoleMessage(
                DateTime.UtcNow,
                level,
                message,
                code);

        _messages.Add(
            consoleMessage);

        MessageWritten?.Invoke(
            consoleMessage);
    }

    public void Clear()
    {
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
}
using System.Text.Json;
using Engine.Core.Commands;

namespace Engine.Core.Replays;

public sealed class ReplayCommandRegistry
{
    private readonly Dictionary<Type, CommandDefinition> _byType = new();

    private readonly Dictionary<string, CommandDefinition> _byId =
        new(StringComparer.Ordinal);

    public void Register<TCommand>(
        string id)
        where TCommand : ICommand
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var type =
            typeof(TCommand);

        if (_byType.ContainsKey(type))
        {
            throw new InvalidOperationException(
                $"Command type '{type.Name}' is already registered.");
        }

        if (_byId.ContainsKey(id))
        {
            throw new InvalidOperationException(
                $"Replay command id '{id}' is already registered.");
        }

        var definition =
            new CommandDefinition(
                id,
                type,
                command =>
                    JsonSerializer.Serialize(
                        command,
                        type),
                payload =>
                {
                    var command =
                        JsonSerializer.Deserialize(
                            payload,
                            type);

                    if (command is not ICommand result)
                    {
                        throw new InvalidDataException(
                            $"Failed to deserialize replay command '{id}'.");
                    }

                    return result;
                });

        _byType.Add(
            type,
            definition);

        _byId.Add(
            id,
            definition);
    }

    internal ReplayCommandData Serialize(
        ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_byType.TryGetValue(
                command.GetType(),
                out var definition))
        {
            throw new InvalidOperationException(
                $"Command '{command.GetType().Name}' " +
                "is not registered for replay.");
        }

        return new ReplayCommandData(
            definition.Id,
            definition.Serialize(command));
    }

    internal ICommand Deserialize(
        ReplayCommandData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (!_byId.TryGetValue(
                data.Type,
                out var definition))
        {
            throw new InvalidDataException(
                $"Replay command type '{data.Type}' " +
                "is not registered.");
        }

        return definition.Deserialize(
            data.Payload);
    }

    public ReplayCommandData SerializeCommand(
    ICommand command)
    {
        return Serialize(command);
    }

    public ICommand DeserializeCommand(
        ReplayCommandData data)
    {
        return Deserialize(data);
    }

    private sealed class CommandDefinition
    {
        public CommandDefinition(
            string id,
            Type type,
            Func<ICommand, string> serialize,
            Func<string, ICommand> deserialize)
        {
            Id = id;
            Type = type;
            Serialize = serialize;
            Deserialize = deserialize;
        }

        public string Id { get; }

        public Type Type { get; }

        public Func<ICommand, string> Serialize { get; }

        public Func<string, ICommand> Deserialize { get; }
    }
}
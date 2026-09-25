using Engine.Core.Commands;
using Engine.Core.Time;

namespace Engine.Core.Replays;

public sealed class ReplayFrame
{
    private readonly List<ICommand> _commands = new();

    internal ReplayFrame(
        Tick tick)
    {
        Tick = tick;
    }

    public Tick Tick { get; }

    public IReadOnlyList<ICommand> Commands =>
        _commands;

    internal void Add(
        ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        _commands.Add(command);
    }
}
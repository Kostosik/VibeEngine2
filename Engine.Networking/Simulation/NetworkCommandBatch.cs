using Engine.Core.Commands;
using Engine.Core.Time;

namespace Engine.Networking.Simulation;

public sealed class NetworkCommandBatch
{
    private readonly List<ICommand> _commands = new();

    public NetworkCommandBatch(
        Tick tick)
    {
        Tick = tick;
    }

    public Tick Tick { get; }

    public IReadOnlyList<ICommand> Commands =>
        _commands;

    public void Add(
        ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        _commands.Add(command);
    }
}
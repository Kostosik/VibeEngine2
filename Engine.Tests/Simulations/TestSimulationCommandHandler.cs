using Engine.Core.Commands;

namespace Engine.Tests.Simulations;

internal sealed class SimulationTestCommandHandler :
    ICommandHandler<SimulationTestCommand>
{
    private readonly List<int> _received;

    public SimulationTestCommandHandler(
        List<int> received)
    {
        _received = received;
    }

    public void Handle(
        SimulationTestCommand command)
    {
        _received.Add(
            command.Value);
    }
}
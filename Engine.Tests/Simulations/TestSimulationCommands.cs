using Engine.Core.Commands;

namespace Engine.Tests.Simulations;

internal sealed record SimulationTestCommand(
    int Value) : ICommand;
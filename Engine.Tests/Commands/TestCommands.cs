using Engine.Core.Commands;

namespace Engine.Tests.Commands;

internal sealed record TestCommand(
    int Value) : ICommand;

internal sealed record SecondTestCommand(
    int Value) : ICommand;

internal sealed class TestCommandHandler :
    ICommandHandler<TestCommand>
{
    private readonly List<int> _received;

    public TestCommandHandler(
        List<int> received)
    {
        _received = received;
    }

    public void Handle(
        TestCommand command)
    {
        _received.Add(
            command.Value);
    }
}

internal sealed class SecondTestCommandHandler :
    ICommandHandler<SecondTestCommand>
{
    private readonly List<int> _received;

    public SecondTestCommandHandler(
        List<int> received)
    {
        _received = received;
    }

    public void Handle(
        SecondTestCommand command)
    {
        _received.Add(
            command.Value);
    }
}

internal sealed class EnqueuingCommandHandler :
    ICommandHandler<TestCommand>
{
    private readonly CommandQueue _queue;

    private readonly List<int> _received;

    public EnqueuingCommandHandler(
        CommandQueue queue,
        List<int> received)
    {
        _queue = queue;
        _received = received;
    }

    public void Handle(
        TestCommand command)
    {
        _received.Add(
            command.Value);

        if (command.Value == 1)
        {
            _queue.Enqueue(
                new TestCommand(2));
        }
    }
}
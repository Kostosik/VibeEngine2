using Engine.Core.Events;
using Engine.Core.Systems;
using Engine.Tests.Events;

namespace Engine.Tests.Simulations;

internal sealed class TestUpdateSystem : IUpdateSystem
{
    private readonly EventBus _events;
    private readonly List<string> _execution;

    public TestUpdateSystem(
        EventBus events,
        List<string> execution)
    {
        _events = events;
        _execution = execution;
    }

    public void Update(
        SystemContext context)
    {
        _execution.Add("Update");

        _events.Publish(
            new TestEvent(1));
    }
}

internal sealed class TestFixedSystem : IFixedUpdateSystem
{
    private readonly EventBus _events;
    private readonly List<string> _execution;

    public TestFixedSystem(
        EventBus events,
        List<string> execution)
    {
        _events = events;
        _execution = execution;
    }

    public void FixedUpdate(
        FixedSystemContext context)
    {
        _execution.Add("FixedUpdate");

        _events.Publish(
            new TestEvent(1));
    }
}
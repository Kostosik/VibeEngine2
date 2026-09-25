using Engine.Core.Events;
using Engine.Core.Systems;

using TestEvent = Engine.Tests.Events.TestEvent;

namespace Engine.Tests.Simulations;

internal sealed class SimulationUpdateTestSystem : IUpdateSystem
{
    private readonly EventBus _events;
    private readonly List<string> _execution;

    public SimulationUpdateTestSystem(
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

internal sealed class SimulationFixedTestSystem : IFixedUpdateSystem
{
    private readonly EventBus _events;
    private readonly List<string> _execution;

    public SimulationFixedTestSystem(
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
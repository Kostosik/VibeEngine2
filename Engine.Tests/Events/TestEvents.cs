using Engine.Core.Events;

namespace Engine.Tests.Events;

internal sealed record TestEvent(
    int Value) : IEvent;

internal sealed record SecondTestEvent(
    int Value) : IEvent;
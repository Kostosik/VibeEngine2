using Engine.Core.Time;
using Engine.ECS;
using Engine.ECS.Entities;

namespace Engine.AI.Perception.Core;

public readonly struct PerceptionContext
{
    public World World { get; }

    public EntityId Observer { get; }

    public TimeSnapshot Time { get; }

    internal PerceptionContext(
        World world,
        EntityId observer,
        TimeSnapshot time)
    {
        World = world;
        Observer = observer;
        Time = time;
    }
}
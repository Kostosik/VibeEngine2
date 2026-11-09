using Engine.Core.Time;
using Engine.ECS;
using Engine.ECS.Entities;

namespace Engine.AI.Core;

public readonly struct AIContext
{
    public World World { get; }

    public EntityId Entity { get; }

    public TimeSnapshot Time { get; }

    public AIAgentState AgentState { get; }

    internal AIContext(
        World world,
        EntityId entity,
        TimeSnapshot time,
        AIAgentState agentState)
    {
        World = world;
        Entity = entity;
        Time = time;
        AgentState = agentState;
    }
}

using Engine.AI.Memory;
using Engine.AI.Perception.Core;
using Engine.Core.Time;
using Engine.ECS;
using Engine.ECS.Entities;

namespace Engine.AI.Core;

public readonly struct AIContext
{
    public AILastKnownTargetMemory LastKnownTargets { get; }
    public World World { get; }

    public EntityId Entity { get; }

    public TimeSnapshot Time { get; }

    public TimeSpan Now => Time.Elapsed.Value;

    public AIAgentState AgentState { get; }

    public PerceptionState? Perception { get; }

    public AIMemory Memory { get; }

    internal AIContext(
        World world,
        EntityId entity,
        TimeSnapshot time,
        AIAgentState agentState,
        PerceptionState? perception,
        AIMemory memory)
    {
        World = world;
        Entity = entity;
        Time = time;
        AgentState = agentState;
        Perception = perception;
        Memory = memory;
        LastKnownTargets = memory.GetOrCreate(
    AIMemoryKeys.LastKnownTargets,
    static () => new AILastKnownTargetMemory());
    }
}

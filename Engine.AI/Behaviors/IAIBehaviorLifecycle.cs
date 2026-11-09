using Engine.AI.Core;

namespace Engine.AI.Behaviors;

public interface IAIBehaviorLifecycle
{
    void OnDetached(AIAgentState agentState);
}
using Engine.AI.BehaviorTrees;
using Engine.AI.BehaviorTrees.Core;
using Engine.AI.DecisionMaking.Utility;
using Engine.AI.Perception.Core;
using Engine.AI.Systems;
using Engine.ECS.Entities;

namespace Engine.AI.Diagnostics;

public sealed record AIAgentDebugSnapshot(
    EntityId Entity,
    string BehaviorType,
    TimeSpan? NextUpdateAt,
    ulong? PerceptionVersion,
    TimeSpan? PerceptionUpdatedAt,
    IReadOnlyList<PerceptionObservation> Observations,
    string? CurrentUtilityAction,
    float? CurrentUtilityScore,
    UtilityDecisionDebugInfo? UtilityDecision,
    string? BehaviorTreeName,
    BehaviorStatus? BehaviorTreeStatus);

public static class AIDiagnostics
{
    public static bool TryCapture(
        AISystem system,
        EntityId entity,
        out AIAgentDebugSnapshot? snapshot)
    {
        ArgumentNullException.ThrowIfNull(system);

        snapshot = null;

        if (!system.TryGetBehavior(
                entity,
                out var behavior) ||
            behavior is null)
        {
            return false;
        }

        if (!system.TryGetAgentState(
                entity,
                out var agentState) ||
            agentState is null)
        {
            return false;
        }

        TimeSpan? nextUpdateAt =
            system.Scheduler.TryGetNextUpdateTime(
                entity,
                out var nextUpdate)
                    ? nextUpdate
                    : null;

        system.Perception.TryGetState(
            entity,
            out var perception);

        IReadOnlyList<PerceptionObservation> observations =
            perception is null
                ? Array.Empty<PerceptionObservation>()
                : Array.AsReadOnly(
                    perception.Observations.ToArray());

        ulong? perceptionVersion =
            perception?.HasUpdated == true
                ? perception.Version
                : null;

        TimeSpan? perceptionUpdatedAt =
            perception?.HasUpdated == true
                ? perception.LastUpdatedAt
                : null;

        UtilityAIBehavior? utilityBehavior =
            behavior as UtilityAIBehavior;

        UtilityAction? currentAction =
            utilityBehavior?.GetCurrentAction(agentState);

        float? currentScore =
            currentAction is null
                ? null
                : utilityBehavior!.GetCurrentScore(agentState);

        UtilityDecisionDebugInfo? utilityDecision =
            utilityBehavior?.GetDebugInfo(agentState);

        BehaviorTree? tree =
            behavior as BehaviorTree ??
            currentAction?.WrappedBehaviorTree;

        snapshot = new AIAgentDebugSnapshot(
            entity,
            behavior.GetType().Name,
            nextUpdateAt,
            perceptionVersion,
            perceptionUpdatedAt,
            observations,
            currentAction?.Name,
            currentScore,
            utilityDecision,
            tree?.Name,
            tree?.GetLastStatus(agentState));

        return true;
    }
}
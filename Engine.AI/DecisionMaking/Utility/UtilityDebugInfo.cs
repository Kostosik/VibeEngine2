using Engine.ECS.Entities;

namespace Engine.AI.DecisionMaking.Utility;

public sealed record UtilityConsiderationDebugInfo(
    string Name,
    float RawScore,
    float CurvedScore,
    float Weight,
    float Contribution);

public sealed record UtilityActionDebugInfo(
    string Name,
    float Score,
    float MinimumScore,
    bool IsOnCooldown,
    bool IsSelected,
    IReadOnlyList<UtilityConsiderationDebugInfo> Considerations);

public sealed record UtilityDecisionDebugInfo(
    EntityId Agent,
    string? SelectedAction,
    float SelectedScore,
    TimeSpan Elapsed,
    IReadOnlyList<UtilityActionDebugInfo> Actions);
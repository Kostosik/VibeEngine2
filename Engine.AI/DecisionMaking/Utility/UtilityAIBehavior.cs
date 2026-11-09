using Engine.AI.Behaviors;
using Engine.AI.Core;

namespace Engine.AI.DecisionMaking.Utility;

public sealed class UtilityAIBehavior :
    IAIBehavior,
    IAIBehaviorLifecycle
{
    private readonly List<UtilityAction> _actions = new();

    private float _minimumSwitchScoreDelta = 0.05f;

    private sealed class AgentRuntimeState
    {
        public readonly Dictionary<UtilityAction, TimeSpan>
            Cooldowns = new();

        public readonly List<UtilityActionDebugInfo>
            DebugActions = new();

        public readonly List<UtilityConsiderationDebugInfo>
            ConsiderationBuffer = new();

        public UtilityAction? CurrentAction;

        public float CurrentScore;

        public UtilityDecisionDebugInfo? LastDebugInfo;
        public AIContext LastContext;

        public bool HasContext;
    }

    public IReadOnlyList<UtilityAction> Actions => _actions;

    public bool EnableDebugInformation { get; set; }

    public float MinimumSwitchScoreDelta
    {
        get => _minimumSwitchScoreDelta;

        set
        {
            if (!float.IsFinite(value) ||
                value < 0f ||
                value > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value));
            }

            _minimumSwitchScoreDelta = value;
        }
    }

    public int ActionCount => _actions.Count;

    public UtilityAIBehavior AddAction(
        UtilityAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (_actions.Contains(action))
        {
            throw new InvalidOperationException(
                "Action is already registered.");
        }

        _actions.Add(action);

        return this;
    }

    public bool RemoveAction(UtilityAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return _actions.Remove(action);
    }

    public void ClearActions()
    {
        _actions.Clear();
    }

    public UtilityAction? GetCurrentAction(
        in AIContext context)
    {
        if (!context.AgentState.TryGet(
                this,
                out AgentRuntimeState? state))
        {
            return null;
        }

        return state.CurrentAction;
    }

    public float GetCurrentScore(
        in AIContext context)
    {
        if (!context.AgentState.TryGet(
                this,
                out AgentRuntimeState? state))
        {
            return 0f;
        }

        return state.CurrentScore;
    }

    public UtilityDecisionDebugInfo? GetDebugInfo(
        in AIContext context)
    {
        if (!context.AgentState.TryGet(
                this,
                out AgentRuntimeState? state))
        {
            return null;
        }

        return state.LastDebugInfo;
    }

    public void Update(in AIContext context)
    {
        var state = context.AgentState.GetOrCreate(
            this,
            static () => new AgentRuntimeState());
        state.LastContext = context;
        state.HasContext = true;
        TimeSpan now = context.Time.Elapsed.Value;

        UtilityAction? bestAction = null;
        float bestScore = 0f;

        bool currentActionValid = false;
        float currentActionScore = 0f;

        state.DebugActions.Clear();

        foreach (var action in _actions)
        {
            state.ConsiderationBuffer.Clear();

            float score = action.Evaluate(
                in context,
                EnableDebugInformation
                    ? state.ConsiderationBuffer
                    : null);

            bool isOnCooldown =
                state.Cooldowns.TryGetValue(
                    action,
                    out var cooldownUntil) &&
                now < cooldownUntil;

            bool validScore =
                score > 0f &&
                score >= action.MinimumScore;

            if (ReferenceEquals(action, state.CurrentAction) &&
                validScore)
            {
                currentActionValid = true;
                currentActionScore = score;
            }

            if (validScore &&
                !isOnCooldown &&
                score > bestScore)
            {
                bestAction = action;
                bestScore = score;
            }

            if (EnableDebugInformation)
            {
                state.DebugActions.Add(
                    new UtilityActionDebugInfo(
                        action.Name,
                        score,
                        action.MinimumScore,
                        isOnCooldown,
                        false,
                        state.ConsiderationBuffer.ToArray()));
            }
        }

        UtilityAction? selectedAction;
        float selectedScore;

        if (currentActionValid)
        {
            bool shouldSwitch =
                bestAction != null &&
                !ReferenceEquals(
                    bestAction,
                    state.CurrentAction) &&
                bestScore >
                    currentActionScore +
                    MinimumSwitchScoreDelta;

            if (shouldSwitch)
            {
                selectedAction = bestAction;
                selectedScore = bestScore;
            }
            else
            {
                selectedAction = state.CurrentAction;
                selectedScore = currentActionScore;
            }
        }
        else
        {
            selectedAction = bestAction;
            selectedScore = bestScore;
        }

        if (!ReferenceEquals(
                state.CurrentAction,
                selectedAction))
        {
            state.CurrentAction?.Exit(in context);

            state.CurrentAction = selectedAction;

            state.CurrentAction?.Enter(in context);
        }

        state.CurrentScore = selectedScore;

        if (EnableDebugInformation)
        {
            for (var i = 0;
                 i < state.DebugActions.Count;
                 i++)
            {
                var item = state.DebugActions[i];

                state.DebugActions[i] = item with
                {
                    IsSelected = selectedAction != null &&
                        i < _actions.Count &&
                        ReferenceEquals(_actions[i], selectedAction)
                };
            }

            state.LastDebugInfo =
                new UtilityDecisionDebugInfo(
                    context.Entity,
                    selectedAction?.Name,
                    selectedScore,
                    now,
                    state.DebugActions.ToArray());
        }
        else
        {
            state.LastDebugInfo = null;
        }

        if (selectedAction == null)
            return;

        bool selectedOnCooldown =
            state.Cooldowns.TryGetValue(
                selectedAction,
                out var selectedCooldownUntil) &&
            now < selectedCooldownUntil;

        if (selectedOnCooldown)
            return;

        selectedAction.Execute(in context);

        TimeSpan cooldown =
            selectedAction.Cooldown.Value;

        if (cooldown > TimeSpan.Zero)
        {
            state.Cooldowns[selectedAction] =
                now + cooldown;
        }
        else
        {
            state.Cooldowns.Remove(selectedAction);
        }
    }

    public void OnDetached(AIAgentState agentState)
    {
        ArgumentNullException.ThrowIfNull(agentState);

        if (!agentState.TryGet(
                this,
                out AgentRuntimeState? state))
        {
            return;
        }

        try
        {
            if (state.HasContext)
            {
                state.CurrentAction?.Exit(
                    in state.LastContext);
            }
        }
        finally
        {
            agentState.Remove(this);
        }
    }

    public UtilityAction? GetCurrentAction(
    AIAgentState agentState)
    {
        ArgumentNullException.ThrowIfNull(agentState);

        return agentState.TryGet(
            this,
            out AgentRuntimeState? state)
                ? state.CurrentAction
                : null;
    }

    public float GetCurrentScore(
        AIAgentState agentState)
    {
        ArgumentNullException.ThrowIfNull(agentState);

        return agentState.TryGet(
            this,
            out AgentRuntimeState? state)
                ? state.CurrentScore
                : 0f;
    }

    public UtilityDecisionDebugInfo? GetDebugInfo(
        AIAgentState agentState)
    {
        ArgumentNullException.ThrowIfNull(agentState);

        return agentState.TryGet(
            this,
            out AgentRuntimeState? state)
                ? state.LastDebugInfo
                : null;
    }
}
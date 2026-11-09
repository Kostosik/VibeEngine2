using Engine.AI.Core;
using Engine.AI.DecisionMaking.Utility;
using Engine.AI.Systems;
using Engine.Core.Time;
using Engine.ECS;

namespace Engine.Tests.AI.Utility;

public sealed class UtilityAITests
{
    [Fact]
    public void UtilityCurves_EvaluateExpectedValues()
    {
        Assert.Equal(
            0.5f,
            UtilityCurves.Linear.Evaluate(0.5f),
            5);

        Assert.Equal(
            0.5f,
            UtilityCurves.SmoothStep.Evaluate(0.5f),
            5);

        Assert.Equal(
            0.5f,
            UtilityCurves.Inverse.Evaluate(0.5f),
            5);

        Assert.Equal(
            0.25f,
            UtilityCurves.Power(2f).Evaluate(0.5f),
            5);

        Assert.Equal(
            MathF.Sqrt(0.5f),
            UtilityCurves.Power(0.5f).Evaluate(0.5f),
            5);
    }

    [Fact]
    public void UtilityCurve_ClampsInvalidResults()
    {
        var curve = new DelegateUtilityCurve(
            static _ => float.NaN);

        Assert.Equal(0f, curve.Evaluate(0.5f));

        var curveAboveRange = new DelegateUtilityCurve(
            static _ => 2f);

        Assert.Equal(1f, curveAboveRange.Evaluate(0.5f));
    }

    [Fact]
    public void UtilityAction_UsesConsiderationWeight()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        var behavior = new UtilityAIBehavior
        {
            EnableDebugInformation = true
        };

        var action = new UtilityAction(
            "WeightedAction",
            (in AIContext context) => { });

        action.AddConsideration(
            "HalfScore",
            new DelegateUtilityConsideration(
                "HalfScore",
                (in AIContext context) => 0.5f),
            weight: 2f);

        behavior.AddAction(action);

        ai.SetBehavior(entity, behavior);

        AITestHelpers.Update(ai, 0.0);

        Assert.True(
            ai.TryGetAgentState(
                entity,
                out var state));

        var debug = behavior.GetDebugInfo(state!);

        Assert.NotNull(debug);

        var actionDebug = Assert.Single(debug.Actions);

        Assert.Equal(
            0.25f,
            actionDebug.Score,
            5);

        Assert.Equal(
            2f,
            Assert.Single(actionDebug.Considerations).Weight);
    }

    [Fact]
    public void UtilityAI_SelectsActionWithHighestScore()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        var behavior = new UtilityAIBehavior
        {
            EnableDebugInformation = true
        };

        AddScoredAction(
            behavior,
            "Attack",
            0.3f);

        AddScoredAction(
            behavior,
            "Flee",
            0.9f);

        ai.SetBehavior(entity, behavior);

        AITestHelpers.Update(ai, 0.0);

        var debug = GetDebugInfo(ai, entity, behavior);

        Assert.Equal("Flee", debug.SelectedAction);
        Assert.Equal(0.9f, debug.SelectedScore, 5);
    }

    [Fact]
    public void UtilityAI_UsesSwitchingThreshold()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        float attackScore = 0.6f;
        float fleeScore = 0.55f;

        string? executedAction = null;

        var behavior = new UtilityAIBehavior
        {
            EnableDebugInformation = true,
            MinimumSwitchScoreDelta = 0.1f
        };

        var attack = new UtilityAction(
            "Attack",
            (in AIContext context) =>
            {
                executedAction = "Attack";
            });

        attack.AddConsideration(
            "AttackScore",
            new DelegateUtilityConsideration(
                "AttackScore",
                (in AIContext context) => attackScore));

        var flee = new UtilityAction(
            "Flee",
            (in AIContext context) =>
            {
                executedAction = "Flee";
            });

        flee.AddConsideration(
            "FleeScore",
            new DelegateUtilityConsideration(
                "FleeScore",
                (in AIContext context) => fleeScore));

        behavior.AddAction(attack);
        behavior.AddAction(flee);

        ai.SetBehavior(entity, behavior);

        AITestHelpers.Update(ai, 0.0, 0);

        Assert.Equal("Attack", executedAction);

        fleeScore = 0.65f;

        AITestHelpers.Update(ai, 0.1, 1);

        Assert.Equal("Attack", executedAction);

        fleeScore = 0.8f;

        AITestHelpers.Update(ai, 0.2, 2);

        Assert.Equal("Flee", executedAction);
    }

    [Fact]
    public void UtilityAction_CooldownIsIndependentForEachAgent()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var first = AITestHelpers.CreateAgent(world);
        var second = AITestHelpers.CreateAgent(world);

        int executions = 0;

        var behavior = new UtilityAIBehavior();

        var action = new UtilityAction(
            "Attack",
            (in AIContext context) =>
            {
                executions++;
            })
        {
            Cooldown = Duration.FromSeconds(2)
        };

        action.AddConsideration(
            "AlwaysAvailable",
            new DelegateUtilityConsideration(
                "AlwaysAvailable",
                (in AIContext context) => 1f));

        behavior.AddAction(action);

        ai.SetBehavior(first, behavior);
        ai.SetBehavior(second, behavior);

        AITestHelpers.Update(ai, 0.0);

        Assert.Equal(2, executions);

        AITestHelpers.Update(ai, 1.0, 1);

        Assert.Equal(2, executions);
    }

    [Fact]
    public void UtilityAI_ProvidesDebugInformation()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        var behavior = new UtilityAIBehavior
        {
            EnableDebugInformation = true
        };

        AddScoredAction(
            behavior,
            "Attack",
            0.75f);

        ai.SetBehavior(entity, behavior);

        AITestHelpers.Update(ai, 0.0);

        var debug = GetDebugInfo(ai, entity, behavior);

        Assert.Equal("Attack", debug.SelectedAction);
        Assert.Single(debug.Actions);

        Assert.True(
            debug.Actions[0].IsSelected);
    }

    [Fact]
    public void UtilityAI_DoesNotSelectActionWithoutConsiderations()
    {
        using var world = new World();
        using var ai = new AISystem(world);

        var entity = AITestHelpers.CreateAgent(world);

        int executions = 0;

        var behavior = new UtilityAIBehavior
        {
            EnableDebugInformation = true
        };

        behavior.AddAction(
            new UtilityAction(
                "InvalidAction",
                (in AIContext context) =>
                {
                    executions++;
                }));

        ai.SetBehavior(entity, behavior);

        AITestHelpers.Update(ai, 0.0);

        Assert.Equal(0, executions);

        var debug = GetDebugInfo(ai, entity, behavior);

        Assert.Null(debug.SelectedAction);
        Assert.Equal(0f, debug.SelectedScore);
    }

    private static void AddScoredAction(
        UtilityAIBehavior behavior,
        string name,
        float score)
    {
        var action = new UtilityAction(
            name,
            (in AIContext context) => { });

        action.AddConsideration(
            name + "Score",
            new DelegateUtilityConsideration(
                name + "Score",
                (in AIContext context) => score));

        behavior.AddAction(action);
    }

    private static UtilityDecisionDebugInfo GetDebugInfo(
        AISystem ai,
        Engine.ECS.Entities.EntityId entity,
        UtilityAIBehavior behavior)
    {
        Assert.True(
            ai.TryGetAgentState(
                entity,
                out var state));

        return Assert.IsType<UtilityDecisionDebugInfo>(
            behavior.GetDebugInfo(state!));
    }
}
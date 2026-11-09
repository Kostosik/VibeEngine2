using Engine.AI.BehaviorTrees;
using Engine.AI.Core;
using Engine.Core.Time;

namespace Engine.AI.DecisionMaking.Utility;

public delegate void UtilityActionHandler(
    in AIContext context);
public delegate void UtilityActionLifecycleHandler(
    in AIContext context);
public sealed class UtilityAction
{
    private readonly List<UtilityConsideration>
        _considerations = new();
    private UtilityActionLifecycleHandler? _onEnter;
    private UtilityActionLifecycleHandler? _onExit;
    private readonly UtilityActionHandler _handler;

    private float _minimumScore = 0.01f;

    private Duration _cooldown = Duration.Zero;

    public string Name { get; }

    public IReadOnlyList<UtilityConsideration>
        Considerations => _considerations;

    public float MinimumScore
    {
        get => _minimumScore;

        set
        {
            if (!float.IsFinite(value) ||
                value < 0f ||
                value > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value));
            }

            _minimumScore = value;
        }
    }

    public Duration Cooldown
    {
        get => _cooldown;

        set
        {
            if (value.IsNegative)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value));
            }

            _cooldown = value;
        }
    }

    public UtilityAction(
        string name,
        UtilityActionHandler handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(handler);

        Name = name;
        _handler = handler;
    }

    public UtilityAction AddConsideration(
        IUtilityConsideration consideration,
        float weight = 1f,
        IUtilityCurve? curve = null)
    {
        ArgumentNullException.ThrowIfNull(consideration);

        string name =
            consideration is DelegateUtilityConsideration named
                ? named.Name
                : consideration.GetType().Name;

        return AddConsideration(
            name,
            consideration,
            weight,
            curve);
    }

    public static UtilityAction FromBehaviorTree(
    string name,
    BehaviorTree tree)
    {
        ArgumentNullException.ThrowIfNull(tree);

        return new UtilityAction(
            name,
            (in AIContext context) =>
                tree.Update(in context))
            .OnExit((in AIContext context) =>
                tree.Abort(context.AgentState));
    }

    public UtilityAction OnEnter(
    UtilityActionLifecycleHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _onEnter = handler;
        return this;
    }

    public UtilityAction OnExit(
        UtilityActionLifecycleHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _onExit = handler;
        return this;
    }

    internal void Enter(in AIContext context)
    {
        _onEnter?.Invoke(in context);
    }

    internal void Exit(in AIContext context)
    {
        _onExit?.Invoke(in context);
    }

    public UtilityAction AddConsideration(
        string name,
        IUtilityConsideration consideration,
        float weight = 1f,
        IUtilityCurve? curve = null)
    {
        _considerations.Add(
            new UtilityConsideration(
                name,
                consideration,
                weight,
                curve));

        return this;
    }

    public bool RemoveConsideration(
        IUtilityConsideration consideration)
    {
        ArgumentNullException.ThrowIfNull(consideration);

        var entry = _considerations.Find(
            item => ReferenceEquals(
                item.Evaluator,
                consideration));

        return entry is not null &&
               _considerations.Remove(entry);
    }

    public bool RemoveConsideration(
        UtilityConsideration consideration)
    {
        ArgumentNullException.ThrowIfNull(consideration);

        return _considerations.Remove(consideration);
    }

    public float Evaluate(in AIContext context)
    {
        return Evaluate(in context, null);
    }

    internal float Evaluate(
        in AIContext context,
        List<UtilityConsiderationDebugInfo>? debugInfo)
    {
        if (_considerations.Count == 0)
            return 0f;

        float score = 1f;
        int evaluatedCount = 0;

        foreach (var consideration in _considerations)
        {
            float rawScore = 0f;
            float curvedScore = 0f;
            float contribution = 1f;

            if (consideration.Weight > 0f)
            {
                rawScore = Normalize(
                    consideration.Evaluator.Evaluate(in context));

                curvedScore = Normalize(
                    consideration.Curve.Evaluate(rawScore));

                contribution = MathF.Pow(
                    curvedScore,
                    consideration.Weight);

                evaluatedCount++;

                score *= contribution;
            }

            debugInfo?.Add(
                new UtilityConsiderationDebugInfo(
                    consideration.Name,
                    rawScore,
                    curvedScore,
                    consideration.Weight,
                    contribution));

            if (score <= 0f)
                return 0f;
        }

        if (evaluatedCount == 0)
            return 0f;

        return Normalize(score);
    }

    public void Execute(in AIContext context)
    {
        _handler(in context);
    }

    private static float Normalize(float value)
    {
        if (!float.IsFinite(value))
            return 0f;

        return Math.Clamp(value, 0f, 1f);
    }
}
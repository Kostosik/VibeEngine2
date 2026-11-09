using Engine.AI.Core;

namespace Engine.AI.DecisionMaking.Utility;

public delegate float UtilityScoreEvaluator(
    in AIContext context);

public sealed class DelegateUtilityConsideration
    : IUtilityConsideration
{
    private readonly UtilityScoreEvaluator _evaluator;

    public string Name { get; }

    public DelegateUtilityConsideration(
        string name,
        UtilityScoreEvaluator evaluator)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(evaluator);

        Name = name;
        _evaluator = evaluator;
    }

    public float Evaluate(in AIContext context)
    {
        return _evaluator(in context);
    }
}
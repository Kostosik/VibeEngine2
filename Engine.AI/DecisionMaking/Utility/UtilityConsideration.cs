using Engine.AI.Core;

namespace Engine.AI.DecisionMaking.Utility;

public sealed class UtilityConsideration
{
    public string Name { get; }

    public IUtilityConsideration Evaluator { get; }

    public IUtilityCurve Curve { get; set; }

    private float _weight = 1f;

    public float Weight
    {
        get => _weight;

        set
        {
            if (!float.IsFinite(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value));
            }

            _weight = value;
        }
    }

    public UtilityConsideration(
        string name,
        IUtilityConsideration evaluator,
        float weight = 1f,
        IUtilityCurve? curve = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(evaluator);

        Name = name;
        Evaluator = evaluator;
        Weight = weight;
        Curve = curve ?? UtilityCurves.Linear;
    }
}
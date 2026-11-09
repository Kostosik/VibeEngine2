namespace Engine.AI.DecisionMaking.Utility;

public interface IUtilityCurve
{
    float Evaluate(float input);
}

public sealed class DelegateUtilityCurve : IUtilityCurve
{
    private readonly Func<float, float> _evaluator;

    public DelegateUtilityCurve(Func<float, float> evaluator)
    {
        ArgumentNullException.ThrowIfNull(evaluator);
        _evaluator = evaluator;
    }

    public float Evaluate(float input)
    {
        input = Normalize(input);

        float result = _evaluator(input);

        return Normalize(result);
    }

    private static float Normalize(float value)
    {
        if (!float.IsFinite(value))
            return 0f;

        return Math.Clamp(value, 0f, 1f);
    }
}

public sealed class PowerUtilityCurve : IUtilityCurve
{
    public float Exponent { get; }

    public PowerUtilityCurve(float exponent)
    {
        if (!float.IsFinite(exponent) || exponent <= 0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(exponent));
        }

        Exponent = exponent;
    }

    public float Evaluate(float input)
    {
        input = Math.Clamp(
            float.IsFinite(input) ? input : 0f,
            0f,
            1f);

        return Math.Clamp(
            MathF.Pow(input, Exponent),
            0f,
            1f);
    }
}

public static class UtilityCurves
{
    public static IUtilityCurve Linear { get; } =
        new DelegateUtilityCurve(static x => x);

    public static IUtilityCurve SmoothStep { get; } =
        new DelegateUtilityCurve(
            static x => x * x * (3f - 2f * x));

    public static IUtilityCurve Inverse { get; } =
        new DelegateUtilityCurve(static x => 1f - x);

    public static IUtilityCurve Power(float exponent)
    {
        return new PowerUtilityCurve(exponent);
    }
}
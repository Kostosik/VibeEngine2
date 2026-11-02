using Engine.Core.Math;

namespace Engine.Editor.UI.Authoring;

internal static class EditorUiCanvasGrid
{
    public const float DefaultStep = 20.0f;
    public const float MinimumStep = 1.0f;
    private static float _step = DefaultStep;

    public static float Step
    {
        get => _step;
        set
        {
            if (!float.IsFinite(value) || value < MinimumStep)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _step = value;
        }
    }

    public static Vector2 SnapPosition(Vector2 position)
    {
        return new Vector2(
            Snap(position.X),
            Snap(position.Y));
    }

    private static float Snap(float value)
    {
        return MathF.Round(
            value / Step,
            MidpointRounding.AwayFromZero) * Step;
    }
}
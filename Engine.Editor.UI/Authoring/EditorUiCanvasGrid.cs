using Engine.Core.Math;

namespace Engine.Editor.UI.Authoring;

internal static class EditorUiCanvasGrid
{
    public const float Step = 20.0f;

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
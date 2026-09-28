using Engine.Core.Math;

namespace Engine.Editor.UI.Authoring;

public readonly record struct EditorUiLayout(
    float X,
    float Y,
    float Width,
    float Height)
{
    public Vector2 Position =>
        new(
            X,
            Y);

    public Vector2 Size =>
        new(
            Width,
            Height);

    public static EditorUiLayout Default =>
        new(
            0.0f,
            0.0f,
            200.0f,
            40.0f);
}
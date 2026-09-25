namespace Engine.Graphics.Fonts;

public readonly record struct FontTextMetrics(
    float Width,
    float Height,
    float Ascent,
    float Descent,
    float LineHeight);
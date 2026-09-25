namespace Engine.Graphics.Fonts;

public readonly record struct FontMetrics(
    float Ascent,
    float Descent,
    float LineGap,
    float LineHeight);
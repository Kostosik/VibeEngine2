using Engine.Core.Math;
using Engine.Graphics.Resources;

namespace Engine.Graphics.Fonts;

public readonly record struct FontGlyph(
    TextureHandle Texture,
    Rectangle UV,
    Vector2 Size,
    Vector2 Bearing,
    float Advance);
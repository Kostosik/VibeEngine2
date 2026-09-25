using Engine.Core.Math;
using Engine.Graphics.Resources;

namespace Engine.Graphics.Sprites;

public readonly record struct Sprite(
    TextureHandle Texture,
    Rectangle UV,
    Vector2 Size,
    int Layer = 0);
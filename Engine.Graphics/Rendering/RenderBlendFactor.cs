namespace Engine.Graphics.Rendering;

public enum RenderBlendFactor
{
    Zero,
    One,

    SourceColor,
    OneMinusSourceColor,

    DestinationColor,
    OneMinusDestinationColor,

    SourceAlpha,
    OneMinusSourceAlpha,

    DestinationAlpha,
    OneMinusDestinationAlpha,

    SourceAlphaSaturate
}
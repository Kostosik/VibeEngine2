namespace Engine.Graphics.Rendering;

public readonly record struct RenderBlendState
{
    public RenderBlendMode Mode { get; init; }

    public RenderBlendFactor SourceColorFactor { get; init; }
    public RenderBlendFactor DestinationColorFactor { get; init; }

    public RenderBlendEquation ColorEquation { get; init; }

    public RenderBlendFactor SourceAlphaFactor { get; init; }
    public RenderBlendFactor DestinationAlphaFactor { get; init; }

    public RenderBlendEquation AlphaEquation { get; init; }

    public RenderBlendState(RenderBlendMode mode)
    {
        Mode = mode;

        ColorEquation = RenderBlendEquation.Add;
        AlphaEquation = RenderBlendEquation.Add;

        switch (mode)
        {
            case RenderBlendMode.Disabled:

                SourceColorFactor = RenderBlendFactor.One;
                DestinationColorFactor = RenderBlendFactor.Zero;

                SourceAlphaFactor = RenderBlendFactor.One;
                DestinationAlphaFactor = RenderBlendFactor.Zero;

                break;

            case RenderBlendMode.Additive:

                SourceColorFactor = RenderBlendFactor.SourceAlpha;
                DestinationColorFactor = RenderBlendFactor.One;

                SourceAlphaFactor = RenderBlendFactor.One;
                DestinationAlphaFactor = RenderBlendFactor.One;

                break;

            case RenderBlendMode.PremultipliedAlpha:

                SourceColorFactor = RenderBlendFactor.One;
                DestinationColorFactor = RenderBlendFactor.OneMinusSourceAlpha;

                SourceAlphaFactor = RenderBlendFactor.One;
                DestinationAlphaFactor = RenderBlendFactor.OneMinusSourceAlpha;

                break;

            case RenderBlendMode.Alpha:
            case RenderBlendMode.Custom:

                SourceColorFactor = RenderBlendFactor.SourceAlpha;
                DestinationColorFactor = RenderBlendFactor.OneMinusSourceAlpha;

                // Сохраняем прежнее поведение OpenGL BlendFunc.
                SourceAlphaFactor = RenderBlendFactor.SourceAlpha;
                DestinationAlphaFactor = RenderBlendFactor.OneMinusSourceAlpha;

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    public static RenderBlendState Default =>
        new(RenderBlendMode.Alpha);

    public static RenderBlendState Additive =>
        new(RenderBlendMode.Additive);

    public static RenderBlendState PremultipliedAlpha =>
        new(RenderBlendMode.PremultipliedAlpha);
}
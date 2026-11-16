using Engine.Graphics.Rendering;

namespace Engine.Tests.Graphics.Rendering;

public sealed class RenderBlendStateTests
{
    [Fact]
    public void Default_UsesLegacyAlphaBlending()
    {
        var state = RenderBlendState.Default;

        Assert.Equal(RenderBlendMode.Alpha, state.Mode);
        Assert.Equal(RenderBlendFactor.SourceAlpha, state.SourceColorFactor);
        Assert.Equal(RenderBlendFactor.OneMinusSourceAlpha, state.DestinationColorFactor);
        Assert.Equal(RenderBlendEquation.Add, state.ColorEquation);

        Assert.Equal(RenderBlendFactor.SourceAlpha, state.SourceAlphaFactor);
        Assert.Equal(RenderBlendFactor.OneMinusSourceAlpha, state.DestinationAlphaFactor);
        Assert.Equal(RenderBlendEquation.Add, state.AlphaEquation);
    }

    [Fact]
    public void Additive_UsesAdditiveFactors()
    {
        var state = RenderBlendState.Additive;

        Assert.Equal(RenderBlendMode.Additive, state.Mode);
        Assert.Equal(RenderBlendFactor.SourceAlpha, state.SourceColorFactor);
        Assert.Equal(RenderBlendFactor.One, state.DestinationColorFactor);
        Assert.Equal(RenderBlendFactor.One, state.DestinationAlphaFactor);
    }

    [Fact]
    public void PremultipliedAlpha_UsesExpectedFactors()
    {
        var state = RenderBlendState.PremultipliedAlpha;

        Assert.Equal(RenderBlendMode.PremultipliedAlpha, state.Mode);
        Assert.Equal(RenderBlendFactor.One, state.SourceColorFactor);
        Assert.Equal(RenderBlendFactor.OneMinusSourceAlpha, state.DestinationColorFactor);
    }

    [Fact]
    public void Custom_CanConfigureColorAndAlphaIndependently()
    {
        var state = RenderBlendState.Default with
        {
            Mode = RenderBlendMode.Custom,
            SourceColorFactor = RenderBlendFactor.One,
            DestinationColorFactor = RenderBlendFactor.One,
            ColorEquation = RenderBlendEquation.Maximum,
            AlphaEquation = RenderBlendEquation.Subtract
        };

        Assert.Equal(RenderBlendMode.Custom, state.Mode);
        Assert.Equal(RenderBlendEquation.Maximum, state.ColorEquation);
        Assert.Equal(RenderBlendEquation.Subtract, state.AlphaEquation);
        Assert.Equal(RenderBlendFactor.One, state.SourceColorFactor);
    }
}
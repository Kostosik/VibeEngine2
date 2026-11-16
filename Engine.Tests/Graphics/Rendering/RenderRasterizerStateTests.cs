using Engine.Graphics.Rendering;

namespace Engine.Tests.Graphics.Rendering;

public sealed class RenderRasterizerStateTests
{
    [Fact]
    public void Default_UsesFilledPolygons()
    {
        var state = RenderRasterizerState.Default;

        Assert.Equal(
            RenderPolygonMode.Fill,
            state.PolygonMode);

        Assert.False(state.PolygonOffset.Enabled);

        Assert.Equal(
            RenderCullMode.Disabled,
            state.CullMode);
    }

    [Fact]
    public void PolygonMode_CanBeChangedToWireframe()
    {
        var state = RenderRasterizerState.Default with
        {
            PolygonMode = RenderPolygonMode.Line
        };

        Assert.Equal(RenderPolygonMode.Line, state.PolygonMode);
    }

    [Fact]
    public void PolygonOffset_CanBeConfigured()
    {
        var state = RenderRasterizerState.Default with
        {
            PolygonOffset = new RenderPolygonOffsetState(
                true,
                1.0f,
                2.0f)
        };

        Assert.True(state.PolygonOffset.Enabled);
        Assert.Equal(1.0f, state.PolygonOffset.Factor);
        Assert.Equal(2.0f, state.PolygonOffset.Units);
    }

    [Fact]
    public void RasterizerState_CanCombineCullAndPolygonMode()
    {
        var state = RenderRasterizerState.Default with
        {
            CullMode = RenderCullMode.Back,
            PolygonMode = RenderPolygonMode.Line
        };

        Assert.Equal(RenderCullMode.Back, state.CullMode);
        Assert.Equal(RenderPolygonMode.Line, state.PolygonMode);
    }
}
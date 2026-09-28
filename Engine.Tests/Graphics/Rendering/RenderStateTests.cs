using Engine.Graphics.Rendering;

namespace Engine.Tests.Graphics.Rendering;

public sealed class RenderStateTests
{
    [Fact]
    public void Default2D_UsesExpected2DState()
    {
        var state =
            RenderState.Default2D;

        Assert.Equal(
            RenderBlendMode.Alpha,
            state.BlendMode);

        Assert.False(
            state.DepthTestEnabled);

        Assert.Equal(
            RenderCullMode.Disabled,
            state.CullMode);
    }
}
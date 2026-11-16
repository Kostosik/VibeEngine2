using Engine.Graphics.Rendering;

namespace Engine.Tests.Graphics.Rendering;

public sealed class RenderStateTests
{
    [Fact]
    public void Default2D_UsesExpected2DState()
    {
        var state = RenderState.Default2D;

        Assert.Equal(RenderBlendMode.Alpha, state.Blend.Mode);

        Assert.False(state.Depth.TestEnabled);
        Assert.True(state.Depth.WriteEnabled);
        Assert.Equal(
            RenderDepthFunction.Less,
            state.Depth.Function);

        Assert.Equal(
            RenderCullMode.Disabled,
            state.Rasterizer.CullMode);

        Assert.Equal(
            RenderFrontFace.CounterClockwise,
            state.Rasterizer.FrontFace);
    }

    [Fact]
    public void LegacyConstructor_CreatesExpectedState()
    {
        var state = new RenderState(
            RenderBlendMode.Disabled,
            true,
            RenderCullMode.Back);

        Assert.Equal(RenderBlendMode.Disabled, state.Blend.Mode);
        Assert.True(state.Depth.TestEnabled);
        Assert.True(state.Depth.WriteEnabled);
        Assert.Equal(RenderCullMode.Back, state.Rasterizer.CullMode);
    }

    [Fact]
    public void DepthState_CanBeModifiedIndependently()
    {
        var state = RenderState.Default2D;

        state = state with
        {
            Depth = state.Depth with
            {
                TestEnabled = true,
                WriteEnabled = false,
                Function = RenderDepthFunction.Greater
            }
        };

        Assert.True(state.Depth.TestEnabled);
        Assert.False(state.Depth.WriteEnabled);
        Assert.Equal(
            RenderDepthFunction.Greater,
            state.Depth.Function);

        Assert.Equal(RenderBlendMode.Alpha, state.Blend.Mode);
        Assert.Equal(
            RenderCullMode.Disabled,
            state.Rasterizer.CullMode);
    }

    [Fact]
    public void Default2D_DisablesScissor()
    {
        var state = RenderState.Default2D;

        Assert.False(state.Scissor.Enabled);
    }

    [Fact]
    public void Scissor_CanBeConfiguredIndependently()
    {
        var state = RenderState.Default2D with
        {
            Scissor = new RenderScissorState(
                true,
                100,
                50,
                400,
                300)
        };

        Assert.True(state.Scissor.Enabled);
        Assert.Equal(100, state.Scissor.X);
        Assert.Equal(50, state.Scissor.Y);
        Assert.Equal(400, state.Scissor.Width);
        Assert.Equal(300, state.Scissor.Height);

        Assert.Equal(RenderBlendMode.Alpha, state.Blend.Mode);
    }

    [Fact]
    public void LegacyProperties_CanStillBeUsedInWithExpression()
    {
        var state = RenderState.Default2D with
        {
            DepthTestEnabled = true,
            CullMode = RenderCullMode.Back
        };

        Assert.True(state.Depth.TestEnabled);
        Assert.Equal(RenderCullMode.Back, state.Rasterizer.CullMode);
    }
}
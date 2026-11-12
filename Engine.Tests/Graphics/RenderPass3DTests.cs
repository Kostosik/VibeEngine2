
using Engine.Graphics.Rendering;
using Engine.Graphics3D.Rendering;
using Xunit;

namespace Engine.Tests.Graphics;

public sealed class RenderPass3DTests
{
    [Fact]
    public void World_EnablesDepthTesting()
    {
        var pass = RenderPass3D.World;

        Assert.True(pass.State.DepthTestEnabled);
    }

    [Fact]
    public void World_EnablesBackFaceCulling()
    {
        var pass = RenderPass3D.World;

        Assert.Equal(
            RenderCullMode.Back,
            pass.State.CullMode);
    }

    [Fact]
    public void World_DisablesBlending()
    {
        var pass = RenderPass3D.World;

        Assert.Equal(
            RenderBlendMode.Disabled,
            pass.State.BlendMode);
    }

    [Fact]
    public void World_ClearsColorAndIsPostProcessSource()
    {
        var pass = RenderPass3D.World;

        Assert.True(pass.ClearColor);
        Assert.True(pass.IsPostProcessSource);
    }

    [Fact]
    public void World_UsesWorldLayerRange()
    {
        var pass = RenderPass3D.World;

        Assert.Equal(
            RenderLayers.World,
            pass.Layers.Minimum);

        Assert.Equal(
            RenderLayers.Present - 1,
            pass.Layers.Maximum);
    }
}

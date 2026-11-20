using Engine.Core.Math;
using Engine.Graphics.Rendering.RenderGraph;
using Engine.Graphics.Resources;
using Xunit;

namespace Engine.Tests.Graphics.Rendering.RenderGraph;

public sealed class RenderGraphClearColorTests
{
    [Fact]
    public void SetClearColor_PreservesColorInCompiledPass()
    {
        var builder = new RenderGraphBuilder();

        var texture = builder.CreateTexture(
            "NormalBuffer",
            new RenderGraphResourceDescriptor(640, 480));

        var expected = new Vector4(0, 0, 1, 1);

        builder.AddPass(
            "ClearNormalBuffer",
            pass =>
            {
                pass.WriteColor(
                    texture,
                    RenderGraphLoadAction.Clear);

                pass.SetClearColor(expected);
            });

        builder.SetOutput(texture);

        var plan = new RenderGraphCompiler().Compile(
            builder.Build());

        var compiledPass = Assert.Single(plan.Passes);

        Assert.Equal(
            expected,
            compiledPass.ClearColorValue);
    }

    [Fact]
    public void SetClearColor_RejectsNonFiniteValues()
    {
        var builder = new RenderGraphBuilder();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => builder.AddPass(
                "InvalidClear",
                pass => pass.SetClearColor(
                    new Vector4(float.NaN, 0, 0, 1))));
    }

    [Fact]
    public void ClearColor_DefaultsToZero()
    {
        var builder = new RenderGraphBuilder();

        var texture = builder.CreateTexture(
            "Scene",
            new RenderGraphResourceDescriptor(640, 480));

        builder.AddPass(
            "Clear",
            pass => pass.WriteColor(
                texture,
                RenderGraphLoadAction.Clear));

        builder.SetOutput(texture);

        var plan = new RenderGraphCompiler().Compile(
            builder.Build());

        Assert.Equal(
            Vector4.Zero,
            Assert.Single(plan.Passes).ClearColorValue);
    }
}
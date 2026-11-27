using Engine.Graphics.OpenGL.Rendering;
using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics.Rendering;

public sealed class OpenGLRenderPassExecutorTests
{
    [Fact]
    public void ResolveRenderDimensions_UsesLogicalSizeForPresentationTarget()
    {
        var presentationTarget = new RenderTargetHandle(1);

        var description = new RenderTargetDescription(
            2560,
            1440);

        var result = OpenGLRenderPassExecutor.ResolveRenderDimensions(
            presentationTarget,
            presentationTarget,
            description,
            1280,
            720);

        Assert.Equal((1280, 720), result);
    }

    [Fact]
    public void ResolveRenderDimensions_UsesTargetSizeForOffscreenTarget()
    {
        var presentationTarget = new RenderTargetHandle(1);
        var offscreenTarget = new RenderTargetHandle(2);

        var description = new RenderTargetDescription(
            512,
            256);

        var result = OpenGLRenderPassExecutor.ResolveRenderDimensions(
            offscreenTarget,
            presentationTarget,
            description,
            1280,
            720);

        Assert.Equal((512, 256), result);
    }
}
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics.Rendering;

public sealed class PostProcessChainTests
{
    [Fact]
    public void AddAndMove_PreserveOrderAndInstances()
    {
        var chain = new PostProcessChain();

        var grayscale = new TestEffect("Grayscale");
        var vignette = new TestEffect("Vignette");
        var tint = new TestEffect("ColorTint");

        chain.Add(grayscale);
        chain.Add(vignette);
        chain.Add(tint);

        vignette.Enabled = false;

        chain.Move(0, 2);

        Assert.Equal(3, chain.Count);

        Assert.Same(vignette, chain.Effects[0]);
        Assert.Same(tint, chain.Effects[1]);
        Assert.Same(grayscale, chain.Effects[2]);

        Assert.False(chain.Effects[0].Enabled);
        Assert.True(chain.Effects[1].Enabled);
    }

    [Fact]
    public void Remove_RemovesEffectFromChain()
    {
        var chain = new PostProcessChain();

        var effect = new TestEffect("Grayscale");

        chain.Add(effect);

        Assert.True(chain.Remove(effect));
        Assert.False(chain.Remove(effect));

        Assert.Empty(chain.Effects);
    }

    [Fact]
    public void RenderPipeline_ExposesPostProcessingChain()
    {
        var pipeline = new RenderPipeline();

        Assert.NotNull(pipeline.PostProcessing);
        Assert.Empty(pipeline.PostProcessing.Effects);
    }

    [Fact]
    public void GrayscaleEffect_UsesShaderAndCanBeDisabled()
    {
        var shader = new ShaderHandle(42);

        var effect = new GrayscaleEffect(shader);

        Assert.Equal("Grayscale", effect.Name);
        Assert.Equal(shader, effect.Shader);
        Assert.True(effect.Enabled);

        effect.Enabled = false;

        Assert.False(effect.Enabled);
    }

    private sealed class TestEffect : IPostProcessEffect
    {
        public TestEffect(string name)
        {
            Name = name;
        }
        public ShaderHandle Shader => ShaderHandle.Invalid;
        public string Name { get; }

        public bool Enabled { get; set; } = true;
    }
}
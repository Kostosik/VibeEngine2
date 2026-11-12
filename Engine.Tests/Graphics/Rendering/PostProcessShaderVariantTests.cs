
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics.Rendering;

public sealed class PostProcessShaderVariantTests
{
    [Theory]
    [InlineData("ColorTint")]
    [InlineData("Grayscale")]
    [InlineData("Vignette")]
    public void BuiltInEffects_ProvideGlslAndHlslVariants(
        string effectName)
    {
        var description = effectName switch
        {
            "ColorTint" =>
                ColorTintEffect.CreateShaderDescription(),

            "Grayscale" =>
                GrayscaleEffect.CreateShaderDescription(),

            "Vignette" =>
                VignetteEffect.CreateShaderDescription(),

            _ => throw new ArgumentOutOfRangeException(
                nameof(effectName))
        };

        var glsl = description.GetVariant(
            ShaderLanguage.Glsl);

        var hlsl = description.GetVariant(
            ShaderLanguage.Hlsl);

        Assert.Contains(
            "#version 330 core",
            glsl.VertexSource);

        Assert.Contains(
            "uniform sampler2D",
            glsl.FragmentSource);

        Assert.Contains(
            "SV_VertexID",
            hlsl.VertexSource);

        Assert.Contains(
            "Texture2D",
            hlsl.FragmentSource);

        Assert.Equal("main", glsl.VertexEntryPoint);
        Assert.Equal("main", glsl.FragmentEntryPoint);

        Assert.Equal("VSMain", hlsl.VertexEntryPoint);
        Assert.Equal("PSMain", hlsl.FragmentEntryPoint);
    }
}

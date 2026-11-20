using Engine.Graphics.Resources;
using Engine.Graphics2D.Lighting;
using Xunit;

namespace Engine.Tests.Graphics2D.Lighting2D;

public sealed class LightingShaderSource2DTests
{
    [Fact]
    public void PointLighting_ProvidesGlslAndHlslVariants()
    {
        var description =
            LightingShaderSource2D.CreatePointLightingDescription();

        var glsl = description.GetVariant(
            ShaderLanguage.Glsl);

        var hlsl = description.GetVariant(
            ShaderLanguage.Hlsl);

        Assert.Contains(
            "uLightPositions[64]",
            glsl.FragmentSource);

        Assert.Contains(
            "uLightIntensities[64]",
            glsl.FragmentSource);

        Assert.Contains(
            "uLightCount",
            hlsl.FragmentSource);
        Assert.Contains(
    "uNormalBuffer",
    glsl.FragmentSource);

        Assert.Contains(
            "dot(surfaceNormal, lightDirection)",
            glsl.FragmentSource);

        Assert.Contains(
            "uNormalBuffer",
            hlsl.FragmentSource);

        Assert.Contains(
            "dot(surfaceNormal, lightDirection)",
            hlsl.FragmentSource);
    }

    [Fact]
    public void AmbientLighting_ProvidesGlslAndHlslVariants()
    {
        var description =
            LightingShaderSource2D.CreateAmbientLightingDescription();

        Assert.True(
            description.TryGetVariant(
                ShaderLanguage.Glsl,
                out _));

        Assert.True(
            description.TryGetVariant(
                ShaderLanguage.Hlsl,
                out _));
    }

    [Fact]
    public void MaxPointLights_Is64()
    {
        Assert.Equal(
            64,
            LightingShaderSource2D.MaxPointLights);
    }
}
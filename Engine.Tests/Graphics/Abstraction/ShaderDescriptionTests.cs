
using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics.Abstraction;

public sealed class ShaderDescriptionTests
{
    [Fact]
    public void LegacyConstructor_CreatesGlslVariant()
    {
        var description = new ShaderDescription(
            "GLSL vertex",
            "GLSL fragment");

        var variant = description.GetVariant(
            ShaderLanguage.Glsl);

        Assert.Equal("GLSL vertex", variant.VertexSource);
        Assert.Equal("GLSL fragment", variant.FragmentSource);
        Assert.Equal("main", variant.VertexEntryPoint);
        Assert.Equal("main", variant.FragmentEntryPoint);
    }

    [Fact]
    public void Constructor_SupportsMultipleLanguages()
    {
        var glsl = new ShaderVariant(
            ShaderLanguage.Glsl,
            "GLSL vertex",
            "GLSL fragment");

        var hlsl = new ShaderVariant(
            ShaderLanguage.Hlsl,
            "HLSL vertex",
            "HLSL pixel",
            "VSMain",
            "PSMain");

        var description = new ShaderDescription(
            glsl,
            hlsl);

        Assert.Equal(
            glsl,
            description.GetVariant(ShaderLanguage.Glsl));

        Assert.Equal(
            hlsl,
            description.GetVariant(ShaderLanguage.Hlsl));
    }

    [Fact]
    public void GetVariant_ThrowsWhenLanguageIsMissing()
    {
        var description = new ShaderDescription(
            "GLSL vertex",
            "GLSL fragment");

        Assert.Throws<NotSupportedException>(
            () => description.GetVariant(ShaderLanguage.Hlsl));
    }

    [Fact]
    public void Constructor_RejectsDuplicateLanguages()
    {
        var first = new ShaderVariant(
            ShaderLanguage.Glsl,
            "Vertex 1",
            "Fragment 1");

        var second = new ShaderVariant(
            ShaderLanguage.Glsl,
            "Vertex 2",
            "Fragment 2");

        Assert.Throws<ArgumentException>(
            () => new ShaderDescription(first, second));
    }

    [Fact]
    public void Constructor_RejectsEmptyVariants()
    {
        Assert.Throws<ArgumentException>(
            () => new ShaderDescription(
                Array.Empty<ShaderVariant>()));
    }

    [Fact]
    public void ShaderVariant_RejectsEmptyEntryPoint()
    {
        Assert.Throws<ArgumentException>(
            () => new ShaderVariant(
                ShaderLanguage.Hlsl,
                "Vertex",
                "Pixel",
                ""));
    }
}

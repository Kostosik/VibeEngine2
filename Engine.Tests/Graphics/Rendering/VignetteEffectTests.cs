using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics.Rendering;

public sealed class VignetteEffectTests
{
    [Fact]
    public void ApplyParameters_WritesAllSettings()
    {
        var effect = new VignetteEffect(
            new ShaderHandle(42))
        {
            Intensity = 0.8f,
            Radius = 0.4f,
            Softness = 0.2f
        };

        var writer = new TestParameterWriter();

        effect.ApplyParameters(writer);

        Assert.Equal(0.8f, writer.Values["uIntensity"]);
        Assert.Equal(0.4f, writer.Values["uRadius"]);
        Assert.Equal(0.2f, writer.Values["uSoftness"]);
    }

    private sealed class TestParameterWriter :
        IPostProcessParameterWriter
    {
        public Dictionary<string, float> Values { get; } = new();

        public void SetFloat(string name, float value)
        {
            Values[name] = value;
        }

        public void SetVector3(
            string name,
            Engine.Core.Math.Vector3 value)
        {
        }
    }
}
using Engine.Core.Math;
using Engine.Graphics.Rendering;
using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics.Rendering;

public sealed class ColorTintEffectTests
{
    [Fact]
    public void ApplyParameters_WritesTintAndIntensity()
    {
        var effect = new ColorTintEffect(new ShaderHandle(42))
        {
            Tint = new Vector3(0.4f, 0.7f, 1.0f),
            Intensity = 0.5f
        };

        var writer = new TestParameterWriter();

        effect.ApplyParameters(writer);

        Assert.Equal(
            new Vector3(0.4f, 0.7f, 1.0f),
            writer.Tint);

        Assert.Equal(0.5f, writer.Intensity);
    }

    [Fact]
    public void Intensity_RejectsValuesOutsideRange()
    {
        var effect = new ColorTintEffect(new ShaderHandle(42));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => effect.Intensity = 1.5f);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => effect.Intensity = -0.1f);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => effect.Intensity = float.NaN);
    }

    private sealed class TestParameterWriter : IPostProcessParameterWriter
    {
        public Vector3 Tint { get; private set; }

        public float Intensity { get; private set; }

        public void SetFloat(string name, float value)
        {
            if (name == "uIntensity")
                Intensity = value;
        }

        public void SetVector3(string name, Vector3 value)
        {
            if (name == "uTint")
                Tint = value;
        }
    }
}
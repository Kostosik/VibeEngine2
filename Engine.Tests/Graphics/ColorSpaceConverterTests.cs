using Engine.Graphics.Resources;

namespace Engine.Tests.Graphics;

public sealed class ColorSpaceConverterTests
{
    [Fact]
    public void SrgbToLinear_DecodesMidGray()
    {
        var result =
            ColorSpaceConverter.SrgbToLinear(0.5f);

        Assert.InRange(
            MathF.Abs(result - 0.21404114f),
            0.0f,
            0.000001f);
    }

    [Fact]
    public void LinearToSrgb_EncodesMidGray()
    {
        var result =
            ColorSpaceConverter.LinearToSrgb(
                0.21404114f);

        Assert.InRange(
            MathF.Abs(result - 0.5f),
            0.0f,
            0.000001f);
    }

    [Fact]
    public void Conversion_RoundTripsMidGray()
    {
        var linear =
            ColorSpaceConverter.SrgbToLinear(0.5f);

        var srgb =
            ColorSpaceConverter.LinearToSrgb(linear);

        Assert.InRange(
            MathF.Abs(srgb - 0.5f),
            0.0f,
            0.000001f);
    }
}
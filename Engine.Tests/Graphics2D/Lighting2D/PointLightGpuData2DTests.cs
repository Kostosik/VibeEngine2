using Engine.Camera;
using Engine.Core.Math;
using Engine.Graphics2D.Lighting;
using Xunit;

namespace Engine.Tests.Graphics2D.Lighting2D;

public sealed class PointLightGpuData2DTests
{
    [Fact]
    public void From_TransformsWorldPositionAndRadius()
    {
        var camera = new Camera2D(
            new Vector2(800, 600))
        {
            Position = new Vector2(100, 50),
            Zoom = 2.0f
        };

        var context = new LightingContext2D(
            camera,
            1600,
            900);

        var light = new Light2D(
            new Vector2(110, 60),
            new Vector3(1.0f, 0.5f, 0.25f),
            20.0f,
            2.0f);

        var result = PointLightGpuData2D.From(
            in light,
            in context);

        Assert.Equal(
            new Vector2(840, 480),
            result.ScreenPosition);

        Assert.Equal(
            new Vector2(40, 30),
            result.ScreenRadius);

        Assert.Equal(light.Color, result.Color);
        Assert.Equal(light.Intensity, result.Intensity);
    }

    [Fact]
    public void From_RejectsDisabledLight()
    {
        var camera = new Camera2D(
            new Vector2(800, 600));

        var context = new LightingContext2D(
            camera,
            800,
            600);

        var light = default(Light2D);

        Assert.Throws<ArgumentException>(
            () => PointLightGpuData2D.From(
                in light,
                in context));
    }
}
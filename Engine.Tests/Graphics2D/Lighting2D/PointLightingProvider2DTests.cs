using Engine.Camera;
using Engine.Core.Math;
using Engine.Graphics2D.Lighting;

public sealed class PointLightingProvider2DTests
{
    private static LightingContext2D CreateContext()
    {
        return new LightingContext2D(
            new Camera2D(new Vector2(1280, 720)),
            1280,
            720);
    }

    private static Light2D CreateLight(
        float x,
        float y,
        float intensity = 1.0f)
    {
        return new Light2D(
            new Vector2(x, y),
            new Vector3(1.0f, 0.8f, 0.6f),
            radius: 150,
            intensity: intensity);
    }

    [Fact]
    public void Provider_SupportsOnlyDirectChannel()
    {
        var provider = new PointLightingProvider2D();

        Assert.Equal(
            LightingChannel2D.Direct,
            provider.SupportedChannels);
    }

    [Fact]
    public void Provider_CollectsMultiplePointLights()
    {
        var lights = new[]
        {
            CreateLight(100, 100),
            CreateLight(200, 200),
            CreateLight(300, 300)
        };

        var registry = new LightingProviderRegistry2D();

        registry.Register(
            new PointLightingProvider2D(lights));

        var result = new LightingCore2D(registry)
            .Collect(CreateContext());

        Assert.Equal(3, result.DirectLighting.Count);

        Assert.Equal(
            lights,
            result.DirectLighting.Cast<Light2D>().ToArray());
    }

    [Fact]
    public void Provider_IgnoresDisabledLights()
    {
        var provider = new PointLightingProvider2D(
        [
            CreateLight(100, 100),
            CreateLight(200, 200, intensity: 0),
            CreateLight(300, 300)
        ]);

        var registry = new LightingProviderRegistry2D();
        registry.Register(provider);

        var result = new LightingCore2D(registry)
            .Collect(CreateContext());

        Assert.Equal(2, result.DirectLighting.Count);
    }

    [Fact]
    public void Provider_AddLight_IncreasesCount()
    {
        var provider = new PointLightingProvider2D();

        provider.AddLight(CreateLight(100, 100));

        Assert.Equal(1, provider.Count);
    }

    [Fact]
    public void Provider_UpdateLight_ChangesPosition()
    {
        var provider = new PointLightingProvider2D(
        [
            CreateLight(100, 100)
        ]);

        provider.UpdateLight(
            0,
            CreateLight(500, 400));

        var snapshot = provider.Lights;

        Assert.Equal(
            new Vector2(500, 400),
            snapshot[0].Position);
    }

    [Fact]
    public void Provider_UpdateLight_UpdatesCollectedResult()
    {
        var provider = new PointLightingProvider2D(
        [
            CreateLight(100, 100)
        ]);

        var registry = new LightingProviderRegistry2D();
        registry.Register(provider);

        var core = new LightingCore2D(registry);

        var firstResult = core.Collect(CreateContext());

        provider.UpdateLight(
            0,
            CreateLight(400, 300));

        var secondResult = core.Collect(CreateContext());

        var firstLight = Assert.IsType<Light2D>(
            Assert.Single(firstResult.DirectLighting));

        var secondLight = Assert.IsType<Light2D>(
            Assert.Single(secondResult.DirectLighting));

        Assert.Equal(
            new Vector2(100, 100),
            firstLight.Position);

        Assert.Equal(
            new Vector2(400, 300),
            secondLight.Position);
    }

    [Fact]
    public void Provider_RemoveLight_DecreasesCount()
    {
        var provider = new PointLightingProvider2D(
        [
            CreateLight(100, 100),
            CreateLight(200, 200)
        ]);

        provider.RemoveLight(0);

        Assert.Single(provider.Lights);
        Assert.Equal(
            new Vector2(200, 200),
            provider.Lights[0].Position);
    }

    [Fact]
    public void Provider_Clear_RemovesAllLights()
    {
        var provider = new PointLightingProvider2D(
        [
            CreateLight(100, 100),
            CreateLight(200, 200)
        ]);

        provider.Clear();

        Assert.Empty(provider.Lights);
        Assert.Equal(0, provider.Count);
    }

    [Fact]
    public void Provider_SetLights_ReplacesCollection()
    {
        var provider = new PointLightingProvider2D(
        [
            CreateLight(100, 100)
        ]);

        provider.SetLights(
        [
            CreateLight(300, 300),
            CreateLight(400, 400),
            CreateLight(500, 500)
        ]);

        Assert.Equal(3, provider.Count);
    }

    [Fact]
    public void Provider_Lights_ReturnsStableSnapshot()
    {
        var provider = new PointLightingProvider2D(
        [
            CreateLight(100, 100)
        ]);

        var snapshot = provider.Lights;

        provider.AddLight(
            CreateLight(200, 200));

        Assert.Single(snapshot);
        Assert.Equal(2, provider.Count);
    }

    [Fact]
    public void Provider_RejectsInvalidUpdateIndex()
    {
        var provider = new PointLightingProvider2D();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => provider.UpdateLight(
                0,
                CreateLight(100, 100)));
    }

    [Fact]
    public void Provider_RejectsInvalidRemoveIndex()
    {
        var provider = new PointLightingProvider2D();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => provider.RemoveLight(0));
    }

    [Fact]
    public void Provider_PreservesHdrIntensity()
    {
        var provider = new PointLightingProvider2D(
        [
            CreateLight(100, 100, intensity: 5.0f)
        ]);

        var registry = new LightingProviderRegistry2D();
        registry.Register(provider);

        var result = new LightingCore2D(registry)
            .Collect(CreateContext());

        var light = Assert.IsType<Light2D>(
            Assert.Single(result.DirectLighting));

        Assert.Equal(5.0f, light.Intensity);
    }
}
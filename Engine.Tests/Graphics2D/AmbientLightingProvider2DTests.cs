using Engine.Camera;
using Engine.Core.Math;
using Engine.Graphics2D.Lighting;

public sealed class AmbientLightingProvider2DTests
{
    private static LightingContext2D CreateContext()
    {
        return new LightingContext2D(
            new Camera2D(new Vector2(1280, 720)),
            1280,
            720);
    }

    [Fact]
    public void Provider_SupportsOnlyAmbientChannel()
    {
        var provider = new AmbientLightingProvider2D(
            new AmbientLight2D(Vector3.One));

        Assert.Equal(
            LightingChannel2D.Ambient,
            provider.SupportedChannels);
    }

    [Fact]
    public void Provider_CollectsAmbientContribution()
    {
        var light = new AmbientLight2D(
            new Vector3(0.2f, 0.4f, 0.6f),
            0.75f);

        var registry = new LightingProviderRegistry2D();

        registry.Register(
            new AmbientLightingProvider2D(light));

        var core = new LightingCore2D(registry);

        var result = core.Collect(CreateContext());

        var contribution = Assert.Single(
            result.AmbientLighting);

        Assert.Equal(light, Assert.IsType<AmbientLight2D>(
            contribution));
    }

    [Fact]
    public void Provider_DisabledLightProducesNoContribution()
    {
        var registry = new LightingProviderRegistry2D();

        registry.Register(
            new AmbientLightingProvider2D(
                AmbientLight2D.Disabled));

        var core = new LightingCore2D(registry);

        var result = core.Collect(CreateContext());

        Assert.Empty(result.Contributions);
        Assert.Empty(result.AmbientLighting);

        Assert.Equal(
            LightingChannel2D.None,
            result.AvailableChannels);
    }

    [Fact]
    public void Provider_UsesUpdatedLightConfiguration()
    {
        var firstLight = new AmbientLight2D(
            new Vector3(0.2f, 0.2f, 0.2f),
            0.5f);

        var secondLight = new AmbientLight2D(
            new Vector3(0.8f, 0.6f, 0.4f),
            1.5f);

        var provider = new AmbientLightingProvider2D(firstLight);

        var registry = new LightingProviderRegistry2D();
        registry.Register(provider);

        var core = new LightingCore2D(registry);

        var firstResult = core.Collect(CreateContext());

        provider.Light = secondLight;

        var secondResult = core.Collect(CreateContext());

        Assert.Equal(
            firstLight,
            Assert.IsType<AmbientLight2D>(
                Assert.Single(firstResult.AmbientLighting)));

        Assert.Equal(
            secondLight,
            Assert.IsType<AmbientLight2D>(
                Assert.Single(secondResult.AmbientLighting)));
    }

    [Fact]
    public void Provider_PreservesHdrIntensity()
    {
        var light = new AmbientLight2D(
            new Vector3(1.0f, 0.8f, 0.5f),
            4.0f);

        var registry = new LightingProviderRegistry2D();

        registry.Register(
            new AmbientLightingProvider2D(light));

        var result = new LightingCore2D(registry)
            .Collect(CreateContext());

        var contribution = Assert.IsType<AmbientLight2D>(
            Assert.Single(result.AmbientLighting));

        Assert.Equal(4.0f, contribution.Intensity);
    }

    [Fact]
    public void Provider_DoesNotModifyOtherChannels()
    {
        var registry = new LightingProviderRegistry2D();

        registry.Register(
            new AmbientLightingProvider2D(
                new AmbientLight2D(Vector3.One)));

        var result = new LightingCore2D(registry)
            .Collect(CreateContext());

        Assert.Empty(result.DirectLighting);
        Assert.Empty(result.IndirectLighting);
        Assert.Empty(result.Emission);
    }
}
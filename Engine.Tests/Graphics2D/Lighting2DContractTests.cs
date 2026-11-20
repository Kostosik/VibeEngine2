using Engine.Camera;
using Engine.Core.Math;
using Engine.Graphics2D.Lighting;

public sealed class Lighting2DProviderTests
{
    private static LightingContext2D CreateContext()
    {
        var camera = new Camera2D(
            new Vector2(800, 600));

        return new LightingContext2D(
            camera,
            800,
            600);
    }

    [Fact]
    public void LightingCore_CollectsSeparateChannels()
    {
        var registry = new LightingProviderRegistry2D();

        var light = new Light2D(
            new Vector2(100, 200),
            new Vector3(1, 0.5f, 0.2f),
            150);

        var ambient = new AmbientLight2D(
            new Vector3(0.2f, 0.2f, 0.3f),
            0.5f);

        registry.Register(
            new TestProvider(
                LightingChannel2D.Direct,
                writer => writer.Add(light)));

        registry.Register(
            new TestProvider(
                LightingChannel2D.Ambient,
                writer => writer.Add(ambient)));

        var core = new LightingCore2D(registry);

        var result = core.Collect(CreateContext());

        Assert.Single(result.DirectLighting);
        Assert.Single(result.AmbientLighting);

        Assert.Empty(result.IndirectLighting);
        Assert.Empty(result.Emission);

        Assert.Equal(
            LightingChannel2D.Direct |
            LightingChannel2D.Ambient,
            result.AvailableChannels);

        Assert.Equal(
            light,
            Assert.IsType<Light2D>(
                result.DirectLighting[0]));

        Assert.Equal(
            ambient,
            Assert.IsType<AmbientLight2D>(
                result.AmbientLighting[0]));
    }

    [Fact]
    public void Registry_RejectsDuplicateProvider()
    {
        var registry = new LightingProviderRegistry2D();

        var provider = new TestProvider(
            LightingChannel2D.Direct,
            _ => { });

        registry.Register(provider);

        Assert.Throws<InvalidOperationException>(
            () => registry.Register(provider));

        Assert.Equal(1, registry.Count);
    }

    [Fact]
    public void Registry_UnregisterRemovesProvider()
    {
        var registry = new LightingProviderRegistry2D();

        var provider = new TestProvider(
            LightingChannel2D.Direct,
            _ => { });

        registry.Register(provider);

        Assert.True(registry.Unregister(provider));
        Assert.False(registry.Unregister(provider));

        Assert.Equal(0, registry.Count);
    }

    [Fact]
    public void Registry_RejectsProviderWithoutChannels()
    {
        var registry = new LightingProviderRegistry2D();

        var provider = new TestProvider(
            LightingChannel2D.None,
            _ => { });

        Assert.Throws<ArgumentOutOfRangeException>(
            () => registry.Register(provider));
    }

    [Fact]
    public void Registry_SnapshotRemainsUnchanged()
    {
        var registry = new LightingProviderRegistry2D();

        registry.Register(
            new TestProvider(
                LightingChannel2D.Direct,
                _ => { }));

        var snapshot = registry.CaptureSnapshot();

        registry.Register(
            new TestProvider(
                LightingChannel2D.Ambient,
                _ => { }));

        Assert.Single(snapshot.Providers);
        Assert.Equal(2, registry.Count);

        Assert.Equal(
            snapshot.Revision + 1,
            registry.Revision);
    }

    [Fact]
    public void LightingCore_UsesSnapshotDuringCollection()
    {
        var registry = new LightingProviderRegistry2D();

        var secondProvider = new TestProvider(
            LightingChannel2D.Ambient,
            writer => writer.Add(
                new AmbientLight2D(Vector3.One)));

        var shouldRegister = true;

        var firstProvider = new TestProvider(
            LightingChannel2D.Direct,
            writer =>
            {
                writer.Add(
                    new Light2D(
                        Vector2.Zero,
                        Vector3.One,
                        100));

                if (shouldRegister)
                {
                    registry.Register(secondProvider);
                    shouldRegister = false;
                }
            });

        registry.Register(firstProvider);

        var core = new LightingCore2D(registry);

        var firstResult = core.Collect(CreateContext());

        Assert.Empty(firstResult.AmbientLighting);

        var secondResult = core.Collect(CreateContext());

        Assert.Single(secondResult.AmbientLighting);
    }

    [Fact]
    public void LightingCore_RejectsUnsupportedContribution()
    {
        var registry = new LightingProviderRegistry2D();

        registry.Register(
            new TestProvider(
                LightingChannel2D.Direct,
                writer => writer.Add(
                    new AmbientLight2D(Vector3.One))));

        var core = new LightingCore2D(registry);

        Assert.Throws<InvalidOperationException>(
            () => core.Collect(CreateContext()));
    }

    [Fact]
    public void LightingCore_RejectsContributionWithMultipleChannels()
    {
        var registry = new LightingProviderRegistry2D();

        registry.Register(
            new TestProvider(
                LightingChannel2D.All,
                writer => writer.Add(
                    new TestContribution(
                        LightingChannel2D.Direct |
                        LightingChannel2D.Ambient))));

        var core = new LightingCore2D(registry);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => core.Collect(CreateContext()));
    }

    private sealed class TestProvider :
        ILightingContributionProvider2D
    {
        private readonly Action<LightingContributionWriter2D> _action;

        public LightingChannel2D SupportedChannels { get; }

        public TestProvider(
            LightingChannel2D supportedChannels,
            Action<LightingContributionWriter2D> action)
        {
            SupportedChannels = supportedChannels;
            _action = action;
        }

        public void Contribute(
            in LightingContext2D context,
            LightingContributionWriter2D contributions)
        {
            _action(contributions);
        }
    }

    private readonly record struct TestContribution(
        LightingChannel2D Channel) : ILightingContribution2D;
}
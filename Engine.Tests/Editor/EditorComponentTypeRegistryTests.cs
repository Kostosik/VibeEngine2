
using Engine.Editor.Inspection;
using Engine.Particles;

namespace Engine.Tests.Editor;

public sealed class EditorComponentTypeRegistryTests
{
    [Fact]
    public void RegisterEngineComponents_RegistersParticleEffectComponent()
    {
        var registry = new EditorComponentTypeRegistry();

        registry.RegisterEngineComponents();

        Assert.True(
            registry.Contains(typeof(ParticleEffectComponent)));

        var component =
            Assert.IsType<ParticleEffectComponent>(
                registry.CreateDefaultValue(
                    typeof(ParticleEffectComponent)));

        Assert.Equal(string.Empty, component.EffectPath);
        Assert.Equal(0, component.InitialBurstCount);
    }

    [Fact]
    public void CreateDefaultValue_UsesRegisteredFactory()
    {
        var registry =
            new EditorComponentTypeRegistry();

        registry.Register<TestComponent>(
            () => new TestComponent
            {
                Value = 42
            });

        var result =
            registry.CreateDefaultValue(
                typeof(TestComponent));

        var component =
            Assert.IsType<TestComponent>(result);

        Assert.Equal(
            42,
            component.Value);
    }

    [Fact]
    public void RegisterWithoutFactory_CreatesDefaultValue()
    {
        var registry =
            new EditorComponentTypeRegistry();

        registry.Register<TestComponent>();

        var result =
            registry.CreateDefaultValue(
                typeof(TestComponent));

        var component =
            Assert.IsType<TestComponent>(result);

        Assert.Equal(
            0,
            component.Value);
    }

    [Fact]
    public void CreateDefaultValue_RejectsUnregisteredType()
    {
        var registry =
            new EditorComponentTypeRegistry();

        Assert.Throws<InvalidOperationException>(
            () => registry.CreateDefaultValue(
                typeof(TestComponent)));
    }

    private struct TestComponent
    {
        public int Value { get; set; }
    }
}

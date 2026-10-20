
using Engine.Editor.Inspection;

namespace Engine.Tests.Editor;

public sealed class EditorComponentTypeRegistryTests
{
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

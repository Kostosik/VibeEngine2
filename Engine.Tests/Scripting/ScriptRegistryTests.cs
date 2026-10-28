using Engine.Scripting;

namespace Engine.Tests.Scripting;

public sealed class ScriptRegistryTests
{
    [Fact]
    public void RegisterDefinition_CreatesConfiguredImplementation()
    {
        var type = typeof(TestScript);

        var definition = new ScriptDefinition(
            ScriptDefinition.CurrentFormatVersion,
            "game.test",
            "{}",
            type.Assembly.GetName().Name!,
            type.FullName!);

        var registry = new ScriptRegistry();

        registry.Register(definition);

        Assert.True(registry.Contains("game.test"));
        Assert.IsType<TestScript>(registry.Create("game.test"));
    }

    [Fact]
    public void RegisterAllowsCreatingScript()
    {
        var registry = new ScriptRegistry();

        registry.Register<TestScript>(
            "game.test");

        var script = registry.Create(
            "game.test");

        Assert.IsType<TestScript>(script);
        Assert.True(registry.Contains("game.test"));
        Assert.Equal(1, registry.Count);
    }

    [Fact]
    public void RegisterRejectsDuplicateId()
    {
        var registry = new ScriptRegistry();

        registry.Register<TestScript>(
            "game.test");

        Assert.Throws<InvalidOperationException>(
            () => registry.Register<TestScript>(
                "game.test"));
    }

    [Fact]
    public void CreateRejectsUnknownId()
    {
        var registry = new ScriptRegistry();

        Assert.Throws<KeyNotFoundException>(
            () => registry.Create("game.missing"));
    }

    [Fact]
    public void RegisterRejectsEmptyId()
    {
        var registry = new ScriptRegistry();

        Assert.Throws<ArgumentException>(
            () => registry.Register<TestScript>(" "));
    }

    [Fact]
    public void RegisteredIdsAreSorted()
    {
        var registry = new ScriptRegistry();

        registry.Register<TestScript>("game.z");
        registry.Register<TestScript>("game.a");

        Assert.Equal(
            new[]
            {
                "game.a",
                "game.z"
            },
            registry.RegisteredIds);
    }

    public sealed class TestScript : IEntityScript
    {
        public TestScript()
        {
        }
    }
}

using Engine.Editor;
using Engine.Editor.Inspection;

namespace Engine.Tests.Editor;

public sealed class EditorComponentTypeRegistryTests
{
    [Fact]
    public void Register_GameComponent_AddsTypeToRegistry()
    {
        var editor = new EditorContext();

        editor.ComponentTypes.Register<GameComponent>();

        Assert.True(
            editor.ComponentTypes.Contains(
                typeof(GameComponent)));

        Assert.Contains(
            typeof(GameComponent),
            editor.ComponentTypes.Types);
    }

    private struct GameComponent
    {
        public int Value { get; set; }
    }
}

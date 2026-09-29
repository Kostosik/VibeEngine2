using Engine.UI.Controls;
using Engine.UI.Input;
using Engine.UI.Screens;

namespace Engine.Tests.UI.Screens;

public sealed class UiScreenManagerTests
{
    [Fact]
    public void Update_UpdatesOnlyCurrentScreen()
    {
        var focus =
            new UiFocusManager();

        var manager =
            new UiScreenManager(
                focus);

        var first =
            new TestScreen();

        var second =
            new TestScreen();

        manager.Replace(
            first);

        manager.Update(
            0.25);

        Assert.Equal(
            1,
            first.UpdateCount);

        manager.Push(
            second);

        manager.Update(
            0.5);

        Assert.Equal(
            1,
            first.UpdateCount);

        Assert.Equal(
            1,
            second.UpdateCount);

        manager.Pop();

        manager.Update(
            1.0);

        Assert.Equal(
            2,
            first.UpdateCount);

        Assert.Equal(
            1,
            second.UpdateCount);
    }

    private sealed class TestScreen :
        UiScreen
    {
        public TestScreen()
            : base(
                new UiPanel())
        {
        }

        public int UpdateCount { get; private set; }

        public override void Update(
            double deltaSeconds)
        {
            UpdateCount++;
        }
    }
}
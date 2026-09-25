using Engine.Input;
using Xunit;

namespace Engine.Tests.Input;

public sealed class InputActionMapTests
{
    [Fact]
    public void Bind_AddsBindingToAction()
    {
        var action = new InputAction("Build");
        var binding = new InputBinding("Mouse.Left");

        var map = new InputActionMap();

        map.Bind(action, binding);

        Assert.True(map.IsBound(action));
    }

    [Fact]
    public void Bind_DoesNotCreateDuplicateBinding()
    {
        var backend = new TestInputBackend();

        var action = new InputAction("Build");
        var binding = new InputBinding("Mouse.Left");

        var map = new InputActionMap();

        map.Bind(action, binding);
        map.Bind(action, binding);

        backend.Set(binding, 1.0f);

        var input = new ActionInput(
            backend,
            map);

        input.Update();

        Assert.Equal(
            1.0f,
            input.GetValue(action));
    }

    [Fact]
    public void Unbind_RemovesAction()
    {
        var action = new InputAction("Build");
        var binding = new InputBinding("Mouse.Left");

        var map = new InputActionMap();

        map.Bind(action, binding);
        map.Unbind(action);

        Assert.False(map.IsBound(action));
    }

    [Fact]
    public void Clear_RemovesAllBindings()
    {
        var action1 = new InputAction("Build");
        var action2 = new InputAction("Move");

        var binding1 = new InputBinding("Mouse.Left");
        var binding2 = new InputBinding("Keyboard.W");

        var map = new InputActionMap();

        map.Bind(action1, binding1);
        map.Bind(action2, binding2);

        map.Clear();

        Assert.False(map.IsBound(action1));
        Assert.False(map.IsBound(action2));
    }
}
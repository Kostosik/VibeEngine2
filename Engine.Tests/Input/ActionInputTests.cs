using Engine.Input;
using Xunit;

namespace Engine.Tests.Input;

public sealed class ActionInputTests
{
    [Fact]
    public void IsDown_ReturnsTrue_WhenBindingIsActive()
    {
        var backend = new TestInputBackend();

        var action = new InputAction("MoveUp");
        var binding = new InputBinding("Keyboard.W");

        var map = new InputActionMap();

        map.Bind(action, binding);

        backend.Set(binding, 1.0f);

        var input = new ActionInput(
            backend,
            map);

        input.Update();

        Assert.True(input.IsDown(action));
    }

    [Fact]
    public void IsPressed_ReturnsTrue_OnTransitionFromUpToDown()
    {
        var backend = new TestInputBackend();

        var action = new InputAction("Build");
        var binding = new InputBinding("Mouse.Left");

        var map = new InputActionMap();

        map.Bind(action, binding);

        var input = new ActionInput(
            backend,
            map);

        input.Update();

        Assert.False(input.IsPressed(action));

        backend.Set(binding, 1.0f);

        input.Update();

        Assert.True(input.IsPressed(action));
        Assert.True(input.IsDown(action));

        input.Update();

        Assert.False(input.IsPressed(action));
        Assert.True(input.IsDown(action));
    }

    [Fact]
    public void IsReleased_ReturnsTrue_OnTransitionFromDownToUp()
    {
        var backend = new TestInputBackend();

        var action = new InputAction("Build");
        var binding = new InputBinding("Mouse.Left");

        var map = new InputActionMap();

        map.Bind(action, binding);

        backend.Set(binding, 1.0f);

        var input = new ActionInput(
            backend,
            map);

        input.Update();

        Assert.False(input.IsReleased(action));

        backend.Clear();

        input.Update();

        Assert.True(input.IsReleased(action));
        Assert.False(input.IsDown(action));

        input.Update();

        Assert.False(input.IsReleased(action));
    }

    [Fact]
    public void GetValue_ReturnsBindingValue()
    {
        var backend = new TestInputBackend();

        var action = new InputAction("Zoom");
        var binding = new InputBinding("Mouse.Scroll");

        var map = new InputActionMap();

        map.Bind(action, binding);

        backend.Set(binding, 2.5f);

        var input = new ActionInput(
            backend,
            map);

        input.Update();

        Assert.Equal(
            2.5f,
            input.GetValue(action));
    }

    [Fact]
    public void Action_CanHaveMultipleBindings()
    {
        var backend = new TestInputBackend();

        var action = new InputAction("MoveUp");

        var keyboardBinding =
            new InputBinding("Keyboard.W");

        var gamepadBinding =
            new InputBinding("Gamepad.LeftStick.Up");

        var map = new InputActionMap();

        map.Bind(action, keyboardBinding);
        map.Bind(action, gamepadBinding);

        backend.Set(
            gamepadBinding,
            0.75f);

        var input = new ActionInput(
            backend,
            map);

        input.Update();

        Assert.Equal(
            0.75f,
            input.GetValue(action));
    }

    [Fact]
    public void MultipleBindings_UseStrongestValue()
    {
        var backend = new TestInputBackend();

        var action = new InputAction("MoveUp");

        var keyboardBinding =
            new InputBinding("Keyboard.W");

        var gamepadBinding =
            new InputBinding("Gamepad.LeftStick.Up");

        var map = new InputActionMap();

        map.Bind(action, keyboardBinding);
        map.Bind(action, gamepadBinding);

        backend.Set(
            keyboardBinding,
            0.4f);

        backend.Set(
            gamepadBinding,
            0.8f);

        var input = new ActionInput(
            backend,
            map);

        input.Update();

        Assert.Equal(
            0.8f,
            input.GetValue(action));
    }

    [Fact]
    public void NegativeAnalogValue_IsPreserved()
    {
        var backend = new TestInputBackend();

        var action = new InputAction("MoveHorizontal");

        var binding =
            new InputBinding("Gamepad.LeftStick.X");

        var map = new InputActionMap();

        map.Bind(action, binding);

        backend.Set(
            binding,
            -0.75f);

        var input = new ActionInput(
            backend,
            map);

        input.Update();

        Assert.Equal(
            -0.75f,
            input.GetValue(action));
    }

    [Fact]
    public void UnboundAction_ReturnsZero()
    {
        var backend = new TestInputBackend();

        var action = new InputAction("Unknown");

        var map = new InputActionMap();

        var input = new ActionInput(
            backend,
            map);

        input.Update();

        Assert.False(input.IsDown(action));
        Assert.False(input.IsPressed(action));
        Assert.False(input.IsReleased(action));
        Assert.Equal(
            0.0f,
            input.GetValue(action));
    }
}
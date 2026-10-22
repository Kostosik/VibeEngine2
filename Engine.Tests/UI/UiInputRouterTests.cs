using Engine.Core.Math;
using Engine.Input;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Tests.UI;

public sealed class UiInputRouterTests
{
    [Fact]
    public void DragDrop_RejectsIncompatibleTarget()
    {
        var root = new UiRoot();

        var source = new TestDragSource
        {
            Width = 40.0f,
            Height = 40.0f,
            HorizontalAlignment = UiHorizontalAlignment.Left,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        var target = new TestDropTarget
        {
            AcceptDrop = false,
            Width = 40.0f,
            Height = 40.0f,
            HorizontalAlignment = UiHorizontalAlignment.Right,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        root.AddChild(source);
        root.AddChild(target);

        root.Arrange(
            new UiRect(0.0f, 0.0f, 100.0f, 100.0f));

        var pointer = new TestPointerInput
        {
            Position = new Vector2(20.0f, 20.0f),
            LeftDown = true,
            LeftPressed = true
        };

        var router = new UiInputRouter(
            root,
            pointer,
            new TestTextInput(),
            new UiFocusManager());

        router.Update();

        // Начинаем перетаскивание.
        pointer.LeftPressed = false;
        pointer.Position = new Vector2(30.0f, 20.0f);
        router.Update();

        Assert.True(router.IsDragging);

        // Перемещаем указатель на цель, которая отклоняет данные.
        pointer.Position = new Vector2(80.0f, 20.0f);
        router.Update();

        // Отпускаем кнопку.
        pointer.LeftDown = false;
        pointer.LeftReleased = true;
        router.Update();

        Assert.Equal(0, target.DropCount);
        Assert.Equal(1, source.DragEndedCount);
        Assert.Equal(
            UiDragEndReason.Rejected,
            source.LastEndReason);

        Assert.False(router.IsDragging);
    }

    [Fact]
    public void DragDrop_ClickWithoutMovement_DoesNotStartDragging()
    {
        var root = new UiRoot();

        var source = new TestDragSource
        {
            Width = 40.0f,
            Height = 40.0f,
            HorizontalAlignment = UiHorizontalAlignment.Left,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        root.AddChild(source);

        root.Arrange(
            new UiRect(0.0f, 0.0f, 100.0f, 100.0f));

        var pointer = new TestPointerInput
        {
            Position = new Vector2(20.0f, 20.0f),
            LeftDown = true,
            LeftPressed = true
        };

        var router = new UiInputRouter(
            root,
            pointer,
            new TestTextInput(),
            new UiFocusManager());

        // 1. Нажимаем на источник.
        router.Update();

        Assert.False(router.IsDragging);
        Assert.Equal(0, source.DragStartedCount);

        // 2. Продолжаем удерживать кнопку, не двигая мышь.
        pointer.LeftPressed = false;
        pointer.LeftReleased = false;
        pointer.LeftDown = true;

        router.Update();

        Assert.False(router.IsDragging);
        Assert.Equal(0, source.DragStartedCount);

        // 3. Отпускаем кнопку.
        pointer.LeftDown = false;
        pointer.LeftReleased = true;

        router.Update();

        Assert.False(router.IsDragging);
        Assert.Equal(0, source.DragStartedCount);
        Assert.Equal(0, source.DragEndedCount);
    }

    [Fact]
    public void DragDrop_SwitchingTargets_UpdatesTargetLifecycle()
    {
        var root = new UiRoot();

        var source = new TestDragSource
        {
            Width = 20.0f,
            Height = 40.0f,
            HorizontalAlignment = UiHorizontalAlignment.Left,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        var firstTarget = new TestDropTarget
        {
            Width = 20.0f,
            Height = 40.0f,
            HorizontalAlignment = UiHorizontalAlignment.Center,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        var secondTarget = new TestDropTarget
        {
            Width = 20.0f,
            Height = 40.0f,
            HorizontalAlignment = UiHorizontalAlignment.Right,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        root.AddChild(source);
        root.AddChild(firstTarget);
        root.AddChild(secondTarget);

        root.Arrange(
            new UiRect(0.0f, 0.0f, 100.0f, 100.0f));

        var pointer = new TestPointerInput
        {
            Position = new Vector2(10.0f, 20.0f),
            LeftDown = true,
            LeftPressed = true
        };

        var router = new UiInputRouter(
            root,
            pointer,
            new TestTextInput(),
            new UiFocusManager());

        // Нажимаем на источник.
        router.Update();

        // Превышаем порог начала перетаскивания.
        pointer.LeftPressed = false;
        pointer.Position = new Vector2(18.0f, 20.0f);
        router.Update();

        Assert.True(router.IsDragging);

        // Наводим указатель на первую цель.
        pointer.Position = new Vector2(50.0f, 20.0f);
        router.Update();

        Assert.Equal(1, firstTarget.DragEnterCount);

        // Переводим указатель на вторую цель.
        pointer.Position = new Vector2(90.0f, 20.0f);
        router.Update();

        Assert.Equal(1, firstTarget.DragLeaveCount);
        Assert.Equal(1, secondTarget.DragEnterCount);

        // Отпускаем кнопку на второй цели.
        pointer.LeftDown = false;
        pointer.LeftReleased = true;
        router.Update();

        Assert.Equal(0, firstTarget.DropCount);
        Assert.Equal(1, secondTarget.DropCount);

        Assert.Equal(1, source.DragEndedCount);
        Assert.Equal(
            UiDragEndReason.Dropped,
            source.LastEndReason);

        Assert.False(router.IsDragging);
    }

    [Fact]
    public void DragDrop_EscapeCancelsActiveDrag()
    {
        var root = new UiRoot();

        var source = new TestDragSource
        {
            Width = 40.0f,
            Height = 40.0f,
            HorizontalAlignment = UiHorizontalAlignment.Left,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        var target = new TestDropTarget
        {
            Width = 40.0f,
            Height = 40.0f,
            HorizontalAlignment = UiHorizontalAlignment.Right,
            VerticalAlignment = UiVerticalAlignment.Top
        };

        root.AddChild(source);
        root.AddChild(target);

        root.Arrange(
            new UiRect(0.0f, 0.0f, 100.0f, 100.0f));

        var pointer = new TestPointerInput
        {
            Position = new Vector2(20.0f, 20.0f),
            LeftDown = true,
            LeftPressed = true
        };

        var textInput = new TestTextInput();

        var router = new UiInputRouter(
            root,
            pointer,
            textInput,
            new UiFocusManager());

        // Нажатие на источник.
        router.Update();

        // Начало перетаскивания.
        pointer.LeftPressed = false;
        pointer.Position = new Vector2(30.0f, 20.0f);
        router.Update();

        Assert.True(router.IsDragging);

        // Перемещение на допустимую цель.
        pointer.Position = new Vector2(80.0f, 20.0f);
        router.Update();

        Assert.True(target.DragEnterCount > 0);

        // Отменяем операцию через Escape.
        textInput.PressedKeys.Add(TextInputKey.Escape);
        router.Update();

        Assert.False(router.IsDragging);

        Assert.Equal(1, source.DragEndedCount);
        Assert.Equal(
            UiDragEndReason.Cancelled,
            source.LastEndReason);

        Assert.Equal(0, target.DropCount);
        Assert.Equal(1, target.DragLeaveCount);
    }

    private sealed class TestTextInput : ITextInput
    {
        public IReadOnlyList<char> Characters =>
            Array.Empty<char>();

        public HashSet<TextInputKey> PressedKeys { get; } = new();

        public bool IsPressed(TextInputKey key)
        {
            return PressedKeys.Contains(key);
        }
    }

    private sealed class TestDragSource :
    UiPanel,
    IUiDragSource
    {
        public int DragStartedCount { get; private set; }

        public int DragEndedCount { get; private set; }

        public UiDragEndReason LastEndReason { get; private set; }

        public UiDragPayload? CreateDragPayload(
            Vector2 startPosition)
        {
            return new UiDragPayload<string>("test-item");
        }

        public void OnDragStarted(
            UiDragContext context)
        {
            DragStartedCount++;
        }

        public void OnDragEnded(
            UiDragContext context,
            UiDragEndReason reason)
        {
            DragEndedCount++;
            LastEndReason = reason;
        }
    }

    private sealed class TestDropTarget :
    UiPanel,
    IUiDropTarget
    {
        public int DragEnterCount { get; private set; }
        public int DragLeaveCount { get; private set; }

        public int DropCount { get; private set; }

        public bool AcceptDrop { get; set; } = true;

        public bool CanAcceptDrop(
            UiDragPayload payload)
        {
            return AcceptDrop &&
                   payload is UiDragPayload<string>;
        }

        public void OnDragLeave(UiDragContext context)
        {
            DragLeaveCount++;
        }

        public void OnDragEnter(
            UiDragContext context)
        {
            DragEnterCount++;
        }

        public bool OnDrop(
            UiDragContext context)
        {
            DropCount++;
            return true;
        }
    }

    [Fact]
    public void RightPointerDownAndUp_AreRoutedToTarget()
    {
        var root =
            new UiRoot();

        var widget =
            new TestWidget();

        root.AddChild(
            widget);

        root.Arrange(
            new UiRect(
                0.0f,
                0.0f,
                100.0f,
                100.0f));

        var pointer =
            new TestPointerInput
            {
                Position =
                    new Vector2(
                        50.0f,
                        50.0f),

                RightPressed = true
            };

        var textInput =
            new TestTextInput();

        var focus =
            new UiFocusManager();

        _ =
            new UiInputRouter(
                root,
                pointer,
                textInput,
                focus);

        pointer.RightPressed =
            true;

        pointer.RightReleased =
            false;

        var router =
            new UiInputRouter(
                root,
                pointer,
                textInput,
                focus);

        router.Update();

        Assert.Equal(
            1,
            widget.PointerDownCount);

        Assert.Equal(
            InputMouseButton.Right,
            widget.LastDownButton);

        pointer.RightPressed =
            false;

        pointer.RightReleased =
            true;

        router.Update();

        Assert.Equal(
            1,
            widget.PointerUpCount);

        Assert.Equal(
            InputMouseButton.Right,
            widget.LastUpButton);
    }

    private sealed class TestWidget :
        UiPanel
    {
        public int PointerDownCount { get; private set; }

        public int PointerUpCount { get; private set; }

        public InputMouseButton LastDownButton { get; private set; }

        public InputMouseButton LastUpButton { get; private set; }

        protected override void OnPointerDown(
            UiPointerEvent pointer)
        {
            PointerDownCount++;
            LastDownButton =
                pointer.Button;
        }

        protected override void OnPointerUp(
            UiPointerEvent pointer)
        {
            PointerUpCount++;
            LastUpButton =
                pointer.Button;
        }
    }

    private sealed class TestPointerInput : IPointerInput
    {
        public Vector2 Position { get; set; }

        public float ScrollDelta => 0.0f;

        public bool LeftDown { get; set; }
        public bool LeftPressed { get; set; }
        public bool LeftReleased { get; set; }

        public bool RightPressed { get; set; }
        public bool RightReleased { get; set; }

        public bool IsDown(InputMouseButton button)
        {
            return button switch
            {
                InputMouseButton.Left => LeftDown,
                InputMouseButton.Right => RightPressed,
                _ => false
            };
        }

        public bool IsPressed(InputMouseButton button)
        {
            return button switch
            {
                InputMouseButton.Left => LeftPressed,
                InputMouseButton.Right => RightPressed,
                _ => false
            };
        }

        public bool IsReleased(InputMouseButton button)
        {
            return button switch
            {
                InputMouseButton.Left => LeftReleased,
                InputMouseButton.Right => RightReleased,
                _ => false
            };
        }
    }
}
using Engine.Input;
using Engine.Input.Cursors;
using Silk.NET.Input;

namespace Engine.Input.SilkNet;

public sealed class SilkNetInputBackend :
    IInputBackend,
    ITextInput,
    IPointerInput,
    ICursorService
{
    private readonly IInputContext _inputContext;

    private IKeyboard? _keyboard;
    private IMouse? _mouse;

    private SilkNetCursorService? _cursorService;

    private float _mouseScroll;
    private float _pendingMouseScroll;

    private bool _currentLeftButton;
    private bool _previousLeftButton;

    private bool _currentRightButton;
    private bool _previousRightButton;

    private bool _currentMiddleButton;
    private bool _previousMiddleButton;

    private readonly List<char> _characters = new();
    private readonly List<char> _pendingCharacters = new();

    private readonly HashSet<Key> _currentTextKeys = new();
    private readonly HashSet<Key> _previousTextKeys = new();

    private static readonly TextInputKey[] TextKeys =
    [
        TextInputKey.F1,
        TextInputKey.Enter,
        TextInputKey.Backspace,
        TextInputKey.Up,
        TextInputKey.Down,
        TextInputKey.Escape
    ];

    public float ScrollDelta =>
    _mouseScroll;

    public SilkNetInputBackend(
        IInputContext inputContext)
    {
        ArgumentNullException.ThrowIfNull(
            inputContext);

        _inputContext =
            inputContext;

        InitializeDevices();
        SubscribeToEvents();

        if (_mouse is not null)
        {
            _cursorService =
                new SilkNetCursorService(
                    _mouse.Cursor);
        }
    }

    public IReadOnlyList<char> Characters =>
        _characters;

    public void Update()
    {
        _characters.Clear();

        if (_pendingCharacters.Count > 0)
        {
            _characters.AddRange(
                _pendingCharacters);

            _pendingCharacters.Clear();
        }

        _previousTextKeys.Clear();

        foreach (var key in _currentTextKeys)
        {
            _previousTextKeys.Add(key);
        }

        _currentTextKeys.Clear();

        UpdateTextKeys();

        _previousLeftButton =
    _currentLeftButton;

        _previousRightButton =
            _currentRightButton;

        _previousMiddleButton =
            _currentMiddleButton;

        if (_mouse is not null)
        {
            _currentLeftButton =
                _mouse.IsButtonPressed(
                    MouseButton.Left);

            _currentRightButton =
                _mouse.IsButtonPressed(
                    MouseButton.Right);

            _currentMiddleButton =
                _mouse.IsButtonPressed(
                    MouseButton.Middle);
        }
        else
        {
            _currentLeftButton = false;
            _currentRightButton = false;
            _currentMiddleButton = false;
        }

        _mouseScroll =
            _pendingMouseScroll;

        _pendingMouseScroll =
            0.0f;
    }

    public bool IsPressed(
        TextInputKey key)
    {
        var silkKey =
            ToSilkKey(key);

        return _currentTextKeys.Contains(silkKey) &&
               !_previousTextKeys.Contains(silkKey);
    }

    public float GetValue(
        InputBinding binding)
    {
        if (!binding.IsValid)
        {
            return 0.0f;
        }

        var path =
            binding.Path;

        if (path.StartsWith(
                "Keyboard.",
                StringComparison.OrdinalIgnoreCase))
        {
            return GetKeyboardValue(
                path["Keyboard.".Length..]);
        }

        if (path.StartsWith(
                "Mouse.",
                StringComparison.OrdinalIgnoreCase))
        {
            return GetMouseValue(
                path["Mouse.".Length..]);
        }

        return 0.0f;
    }

    private void UpdateTextKeys()
    {
        if (_keyboard is null)
        {
            return;
        }

        foreach (var key in TextKeys)
        {
            var silkKey =
                ToSilkKey(key);

            if (_keyboard.IsKeyPressed(
                    silkKey))
            {
                _currentTextKeys.Add(
                    silkKey);
            }
        }
    }

    private static Key ToSilkKey(
        TextInputKey key)
    {
        return key switch
        {
            TextInputKey.F1 =>
                Key.F1,

            TextInputKey.Enter =>
                Key.Enter,

            TextInputKey.Backspace =>
                Key.Backspace,

            TextInputKey.Up =>
                Key.Up,

            TextInputKey.Down =>
                Key.Down,

            TextInputKey.Escape =>
                Key.Escape,

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(key),
                    key,
                    null)
        };
    }

    public Engine.Core.Math.Vector2 Position
    {
        get
        {
            if (_mouse is null)
            {
                return Engine.Core.Math.Vector2.Zero;
            }

            var position =
                _mouse.Position;

            return new Engine.Core.Math.Vector2(
                position.X,
                position.Y);
        }
    }

    public bool IsDown(
        InputMouseButton button)
    {
        return GetCurrentButtonState(
            button);
    }

    public bool IsPressed(
        InputMouseButton button)
    {
        return GetCurrentButtonState(button) &&
               !GetPreviousButtonState(button);
    }

    public bool IsReleased(
        InputMouseButton button)
    {
        return !GetCurrentButtonState(button) &&
               GetPreviousButtonState(button);
    }

    private bool GetCurrentButtonState(
        InputMouseButton button)
    {
        return button switch
        {
            InputMouseButton.Left =>
                _currentLeftButton,

            InputMouseButton.Right =>
                _currentRightButton,

            InputMouseButton.Middle =>
                _currentMiddleButton,

            _ =>
                false
        };
    }

    private bool GetPreviousButtonState(
        InputMouseButton button)
    {
        return button switch
        {
            InputMouseButton.Left =>
                _previousLeftButton,

            InputMouseButton.Right =>
                _previousRightButton,

            InputMouseButton.Middle =>
                _previousMiddleButton,

            _ =>
                false
        };
    }

    private float GetKeyboardValue(
        string keyName)
    {
        if (_keyboard is null)
        {
            return 0.0f;
        }

        if (!Enum.TryParse<Key>(
                keyName,
                true,
                out var key))
        {
            return 0.0f;
        }

        return _keyboard.IsKeyPressed(key)
            ? 1.0f
            : 0.0f;
    }

    private float GetMouseValue(
        string controlName)
    {
        if (_mouse is null)
        {
            return 0.0f;
        }

        if (controlName.Equals(
                "Scroll",
                StringComparison.OrdinalIgnoreCase))
        {
            return _mouseScroll;
        }

        if (!Enum.TryParse<MouseButton>(
                controlName,
                true,
                out var button))
        {
            return 0.0f;
        }

        return _mouse.IsButtonPressed(button)
            ? 1.0f
            : 0.0f;
    }

    private void InitializeDevices()
    {
        if (_inputContext.Keyboards.Count > 0)
        {
            _keyboard =
                _inputContext.Keyboards[0];
        }

        if (_inputContext.Mice.Count > 0)
        {
            _mouse =
                _inputContext.Mice[0];
        }
    }

    private void SubscribeToEvents()
    {
        if (_keyboard is not null)
        {
            _keyboard.KeyChar +=
                OnKeyChar;
        }

        if (_mouse is not null)
        {
            _mouse.Scroll +=
                OnMouseScroll;
        }
    }

    private void OnKeyChar(
        IKeyboard keyboard,
        char character)
    {
        _pendingCharacters.Add(
            character);
    }

    private void OnMouseScroll(
        IMouse mouse,
        ScrollWheel scroll)
    {
        _pendingMouseScroll +=
            scroll.Y;
    }

    public CursorShape Current =>
    _cursorService?.Current ??
    CursorShape.Default;

    public void Set(
        CursorShape shape)
    {
        _cursorService?.Set(
            shape);
    }
}
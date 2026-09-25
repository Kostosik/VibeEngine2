using Engine.Graphics;
using Engine.Graphics.Commands;
using Engine.Graphics.Debug;
using Engine.Input;

namespace Engine.Tooling.Debugging;

public sealed class DebugConsoleOverlay
{
    private const float ConsoleHeight = 360.0f;
    private const float Padding = 8.0f;
    private const float TextScale = 2.0f;
    private const float LineHeight = 20.0f;

    private const int MaximumInputLength = 256;

    private readonly DebugConsole _console;
    private readonly ITextInput _textInput;
    private readonly IInput _input;
    private readonly InputAction _scrollAction;
    private readonly IGraphicsDevice _graphics;

    private float _width;
    private float _height;

    private string _inputText = string.Empty;

    private int _historyIndex = -1;
    private string _historyDraft = string.Empty;

    private int _outputScrollOffset;

    public DebugConsoleOverlay(
        DebugConsole console,
        ITextInput textInput,
        IInput input,
        InputAction scrollAction,
        IGraphicsDevice graphics,
        float width,
        float height)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(textInput);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(scrollAction);
        ArgumentNullException.ThrowIfNull(graphics);

        if (width <= 0.0f)
            throw new ArgumentOutOfRangeException(
                nameof(width));

        if (height <= 0.0f)
            throw new ArgumentOutOfRangeException(
                nameof(height));

        _console = console;
        _textInput = textInput;
        _input = input;
        _scrollAction = scrollAction;
        _graphics = graphics;

        _width = width;
        _height = height;
    }

    public bool IsOpen { get; private set; }

    public void Resize(
        int width,
        int height)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(width));

        if (height <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(height));

        _width = width;
        _height = height;
    }

    public void Update()
    {
        if (_textInput.IsPressed(
                TextInputKey.F1))
        {
            IsOpen =
                !IsOpen;

            if (IsOpen)
            {
                _outputScrollOffset = 0;
                ResetHistoryNavigation();
            }

            return;
        }

        if (!IsOpen)
        {
            return;
        }

        if (_textInput.IsPressed(
                TextInputKey.Escape))
        {
            IsOpen = false;
            return;
        }

        UpdateOutputScroll();

        if (_textInput.IsPressed(
                TextInputKey.Up))
        {
            NavigateHistoryUp();
        }

        if (_textInput.IsPressed(
                TextInputKey.Down))
        {
            NavigateHistoryDown();
        }

        if (_textInput.IsPressed(
                TextInputKey.Backspace))
        {
            if (_inputText.Length > 0)
            {
                _inputText =
                    _inputText[..^1];
            }
        }

        foreach (var character in _textInput.Characters)
        {
            if (char.IsControl(character))
            {
                continue;
            }

            if (_inputText.Length >=
                MaximumInputLength)
            {
                break;
            }

            _inputText +=
                character;
        }

        if (_textInput.IsPressed(
                TextInputKey.Enter))
        {
            ExecuteInput();
        }
    }

    public void Render()
    {
        if (!IsOpen)
        {
            return;
        }

        var consoleHeight =
            MathF.Min(
                ConsoleHeight,
                _height * 0.6f);

        _graphics.Submit(
            new DrawDebugRectangleCommand(
                new Engine.Core.Math.Vector2(
                    0.0f,
                    0.0f),
                new Engine.Core.Math.Vector2(
                    _width,
                    consoleHeight),
                new DebugColor(
                    8,
                    8,
                    8,
                    235),
                true,
                DebugRenderSpace.Screen,
                10000));

        var maximumLines =
            Math.Max(
                1,
                (int)(
                    (consoleHeight -
                     Padding * 2.0f -
                     LineHeight) /
                    LineHeight));

        var lines =
            BuildMessageLines(
                maximumLines);

        var y =
            Padding;

        foreach (var line in lines)
        {
            _graphics.Submit(
                new DrawDebugTextCommand(
                    line,
                    new Engine.Core.Math.Vector2(
                        Padding,
                        y),
                    DebugColor.White,
                    TextScale,
                    DebugRenderSpace.Screen,
                    10001));

            y +=
                LineHeight;
        }

        var promptY =
            consoleHeight -
            LineHeight -
            Padding;

        _graphics.Submit(
            new DrawDebugTextCommand(
                $"> {_inputText}_",
                new Engine.Core.Math.Vector2(
                    Padding,
                    promptY),
                DebugColor.White,
                TextScale,
                DebugRenderSpace.Screen,
                10002));
    }

    private void UpdateOutputScroll()
    {
        var scroll =
            _input.GetValue(
                _scrollAction);

        if (MathF.Abs(scroll) < float.Epsilon)
        {
            return;
        }

        var amount =
            Math.Max(
                1,
                (int)MathF.Ceiling(
                    MathF.Abs(scroll)));

        if (scroll > 0.0f)
        {
            _outputScrollOffset += amount;
        }
        else
        {
            _outputScrollOffset -= amount;
        }
    }

    private void NavigateHistoryUp()
    {
        var entries =
            _console.History.Entries;

        if (entries.Count == 0)
        {
            return;
        }

        if (_historyIndex == -1)
        {
            _historyDraft =
                _inputText;

            _historyIndex =
                entries.Count - 1;
        }
        else if (_historyIndex > 0)
        {
            _historyIndex--;
        }

        _inputText =
            entries[_historyIndex];
    }

    private void NavigateHistoryDown()
    {
        var entries =
            _console.History.Entries;

        if (_historyIndex == -1)
        {
            return;
        }

        if (_historyIndex < entries.Count - 1)
        {
            _historyIndex++;

            _inputText =
                entries[_historyIndex];

            return;
        }

        _historyIndex =
            -1;

        _inputText =
            _historyDraft;
    }

    private IReadOnlyList<string> BuildMessageLines(
        int maximumLines)
    {
        var lines =
            new List<string>();

        var messages =
            _console.Messages;

        foreach (var message in messages)
        {
            var text =
                message.Message
                    .Replace(
                        "\r\n",
                        "\n")
                    .Replace(
                        '\r',
                        '\n');

            var logicalLines =
                text.Split(
                    '\n');

            foreach (var logicalLine in logicalLines)
            {
                var wrapped =
                    WrapText(
                        $"[{message.Level}] {logicalLine}",
                        _width);

                foreach (var line in wrapped)
                {
                    lines.Add(line);
                }
            }
        }

        var maximumScroll =
            Math.Max(
                0,
                lines.Count -
                maximumLines);

        _outputScrollOffset =
            Math.Clamp(
                _outputScrollOffset,
                0,
                maximumScroll);

        if (lines.Count == 0)
        {
            return Array.Empty<string>();
        }

        var end =
            lines.Count -
            _outputScrollOffset;

        var start =
            Math.Max(
                0,
                end - maximumLines);

        return lines
            .Skip(start)
            .Take(end - start)
            .ToArray();
    }

    private static IReadOnlyList<string> WrapText(
        string text,
        float width)
    {
        const float CharacterWidth = 12.0f;

        var maximumCharacters =
            Math.Max(
                1,
                (int)(
                    (width -
                     Padding * 2.0f) /
                    CharacterWidth));

        if (text.Length <= maximumCharacters)
        {
            return new[]
            {
                text
            };
        }

        var lines =
            new List<string>();

        var remaining =
            text;

        while (remaining.Length >
               maximumCharacters)
        {
            var breakIndex =
                remaining.LastIndexOf(
                    ' ',
                    maximumCharacters - 1,
                    maximumCharacters);

            if (breakIndex <= 0)
            {
                breakIndex =
                    maximumCharacters;
            }

            var line =
                remaining[..breakIndex]
                    .TrimEnd();

            if (line.Length == 0)
            {
                line =
                    remaining[..maximumCharacters];
            }

            lines.Add(line);

            remaining =
                remaining[breakIndex..]
                    .TrimStart();
        }

        if (remaining.Length > 0)
        {
            lines.Add(
                remaining);
        }

        return lines;
    }

    private void ExecuteInput()
    {
        var input =
            _inputText.Trim();

        _inputText =
            string.Empty;

        ResetHistoryNavigation();

        _outputScrollOffset =
            0;

        if (input.Length == 0)
        {
            return;
        }

        _console.Execute(
            input);
    }

    private void ResetHistoryNavigation()
    {
        _historyIndex = -1;
        _historyDraft = string.Empty;
    }
}
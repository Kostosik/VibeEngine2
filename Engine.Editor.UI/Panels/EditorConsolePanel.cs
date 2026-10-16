using Engine.Core.Diagnostics;
using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Input;
using Engine.Tooling.Console;
using Engine.Tooling.Debugging;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Panels;

public sealed class EditorConsolePanel : UiPanel
{
    private const float HeaderHeight = 28.0f;
    private const float FilterHeight = 32.0f;
    private const float CommandHeight = 36.0f;
    private const float Gap = 6.0f;
    private const float PaddingSize = 8.0f;
    private bool _messagesDirty;
    private readonly DebugConsole? _console;

    private readonly UiStackPanel _header;
    private readonly UiStackPanel _filters;
    private readonly UiScrollView _outputScrollView;
    private readonly ConsoleOutputView _output;

    private readonly UiLabel _countLabel;
    private readonly UiTextBox _searchBox;
    private readonly UiTextBox _commandInput;

    private readonly UiToggle _verboseToggle;
    private readonly UiToggle _infoToggle;
    private readonly UiToggle _warningToggle;
    private readonly UiToggle _errorToggle;

    private int _historyIndex = -1;
    private string _historyDraft = string.Empty;

    public EditorConsolePanel(DebugConsole? console)
    {
        _console = console;

        Background =
            new UiColor(22, 22, 22, 255);

        Padding =
            new UiThickness(PaddingSize);

        _header =
            new UiStackPanel
            {
                Orientation = UiOrientation.Horizontal,
                Spacing = 8.0f
            };

        _countLabel =
            new UiLabel("0 / 0")
            {
                Width = 120.0f,
                Height = HeaderHeight,
                FontSize = 13.0f,
                Color = new UiColor(165, 170, 180, 255)
            };

        var title =
            new UiLabel("Console")
            {
                Width = 90.0f,
                Height = HeaderHeight,
                FontSize = 15.0f,
                Color = new UiColor(230, 232, 237, 255)
            };

        var clearButton =
            new UiButton("Clear")
            {
                Width = 72.0f,
                Height = HeaderHeight,
                Focusable = false
            };

        clearButton.Clicked += ClearMessages;

        _header.AddChild(title);
        _header.AddChild(_countLabel);
        _header.AddChild(clearButton);

        _verboseToggle =
            new UiToggle("Verbose")
            {
                Width = 104.0f,
                Height = FilterHeight
            };

        _infoToggle =
            new UiToggle("Info")
            {
                Width = 72.0f,
                Height = FilterHeight
            };

        _warningToggle =
            new UiToggle("Warning")
            {
                Width = 100.0f,
                Height = FilterHeight
            };

        _errorToggle =
            new UiToggle("Error")
            {
                Width = 76.0f,
                Height = FilterHeight
            };

        // The initial values are set before subscribing,
        // so initializing filters does not rebuild the log repeatedly.
        _verboseToggle.SetValue(true);
        _infoToggle.SetValue(true);
        _warningToggle.SetValue(true);
        _errorToggle.SetValue(true);

        _searchBox =
            new UiTextBox
            {
                Width = 240.0f,
                Height = FilterHeight,
                FontSize = 13.0f,
                Placeholder = "Filter messages..."
            };

        _filters =
            new UiStackPanel
            {
                Orientation = UiOrientation.Horizontal,
                Spacing = 4.0f
            };

        _filters.AddChild(_verboseToggle);
        _filters.AddChild(_infoToggle);
        _filters.AddChild(_warningToggle);
        _filters.AddChild(_errorToggle);
        _filters.AddChild(_searchBox);

        _output =
            new ConsoleOutputView();

        _outputScrollView =
            new UiScrollView
            {
                VerticalScrolling = true,
                HorizontalScrolling = false,
                ShowVerticalScrollBar = true,
                ShowHorizontalScrollBar = false,
                ScrollSpeed = 36.0f,
                Background = new UiColor(14, 14, 14, 255)
            };

        _outputScrollView.SetContent(_output);

        var prompt =
            new UiLabel(">")
            {
                Width = 22.0f,
                Height = CommandHeight,
                FontSize = 16.0f,
                Color = new UiColor(150, 200, 165, 255),
                VerticalAlignment = UiVerticalAlignment.Center
            };

        _commandInput =
            new UiTextBox
            {
                Width = null,
                Height = CommandHeight,
                FontSize = 14.0f,
                MaxLength = 1024,
                Placeholder = "Enter a command and press Enter"
            };

        _commandInput.Submitted +=
            _ => SubmitCommand();

        _commandInput.PreviewKeyEvent +=
            HandleCommandKeyEvent;

        var runButton =
            new UiButton("Run")
            {
                Width = 72.0f,
                Height = CommandHeight,
                Focusable = false
            };

        runButton.Clicked += SubmitCommand;

        AddChild(_header);
        AddChild(_filters);
        AddChild(_outputScrollView);
        AddChild(prompt);
        AddChild(_commandInput);
        AddChild(runButton);

        _verboseToggle.ValueChanged +=
            _ => RefreshMessages();

        _infoToggle.ValueChanged +=
            _ => RefreshMessages();

        _warningToggle.ValueChanged +=
            _ => RefreshMessages();

        _errorToggle.ValueChanged +=
            _ => RefreshMessages();

        _searchBox.TextChanged +=
            _ => RefreshMessages();

        if (_console is not null)
        {
            _console.MessageWritten +=
                OnMessageWritten;
        }

        RefreshMessages(forceScrollToBottom: true);
    }

    public UiTextBox CommandInput =>
        _commandInput;

    protected override Vector2 MeasureCore(
        UiLayoutContext context,
        Vector2 availableSize)
    {
        var contentWidth =
            MathF.Max(
                0.0f,
                availableSize.X -
                Padding.Left -
                Padding.Right);

        _header.Measure(
            context,
            new Vector2(contentWidth, HeaderHeight));

        _filters.Measure(
            context,
            new Vector2(contentWidth, FilterHeight));

        _outputScrollView.Measure(
            context,
            availableSize);

        _commandInput.Measure(
            context,
            new Vector2(contentWidth - 110.0f, CommandHeight));

        return availableSize;
    }

    protected override void ArrangeCore(
        UiRect finalRect)
    {
        var content =
            finalRect.Deflate(Padding);

        _header.Arrange(
            new UiRect(
                content.X,
                content.Y,
                content.Width,
                HeaderHeight));

        var filterY =
            content.Y + HeaderHeight + Gap;

        _filters.Arrange(
            new UiRect(
                content.X,
                filterY,
                content.Width,
                FilterHeight));

        var commandY =
            content.Bottom - CommandHeight;

        var outputY =
            filterY + FilterHeight + Gap;

        var outputHeight =
            MathF.Max(
                0.0f,
                commandY - Gap - outputY);

        _outputScrollView.Arrange(
            new UiRect(
                content.X,
                outputY,
                content.Width,
                outputHeight));

        var promptWidth = 22.0f;
        var runWidth = 72.0f;
        var commandGap = 6.0f;

        var commandX =
            content.X + promptWidth + commandGap;

        var runX =
            content.Right - runWidth;

        var commandWidth =
            MathF.Max(
                0.0f,
                runX - commandGap - commandX);

        // Find the prompt and Run controls by their stable
        // positions in the child collection.
        Children[3].Arrange(
            new UiRect(
                content.X,
                commandY,
                promptWidth,
                CommandHeight));

        _commandInput.Arrange(
            new UiRect(
                commandX,
                commandY,
                commandWidth,
                CommandHeight));

        Children[5].Arrange(
            new UiRect(
                runX,
                commandY,
                runWidth,
                CommandHeight));
    }

    private void OnMessageWritten(
        ConsoleMessage message)
    {
        _messagesDirty = true;
    }

    private void ClearMessages()
    {
        _console?.Clear();

        RefreshMessages(
            forceScrollToBottom: true);
    }

    private void RefreshMessages(
        bool forceScrollToBottom = false)
    {
        var allMessages =
            _console?.Messages
            ?? Array.Empty<ConsoleMessage>();

        var query =
            _searchBox.Text.Trim();

        var visibleMessages =
            allMessages
                .Where(IsLevelVisible)
                .Where(
                    message =>
                        query.Length == 0 ||
                        message.Message.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase) ||
                        (message.Code?.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase) ?? false))
                .ToArray();

        _countLabel.Text =
            $"{visibleMessages.Length} / {allMessages.Count}";

        var emptyMessage =
            _console is null
                ? "Console backend is not configured."
                : allMessages.Count == 0
                    ? "No messages yet."
                    : "No messages match the current filter.";

        _output.SetMessages(
            visibleMessages,
            emptyMessage,
            forceScrollToBottom);
    }

    private bool IsLevelVisible(
        ConsoleMessage message)
    {
        return message.Level switch
        {
            DiagnosticLevel.Trace or
            DiagnosticLevel.Debug =>
                _verboseToggle.IsOn,

            DiagnosticLevel.Info =>
                _infoToggle.IsOn,

            DiagnosticLevel.Warning =>
                _warningToggle.IsOn,

            DiagnosticLevel.Error or
            DiagnosticLevel.Critical =>
                _errorToggle.IsOn,

            _ => true
        };
    }

    private void SubmitCommand()
    {
        if (_console is null)
        {
            return;
        }

        var command =
            _commandInput.Text.Trim();

        if (command.Length == 0)
        {
            return;
        }

        _commandInput.SetText(string.Empty);

        _historyIndex = -1;
        _historyDraft = string.Empty;

        _console.Write(
            DiagnosticLevel.Debug,
            $"> {command}");

        _console.Execute(command);
    }

    private void HandleCommandKeyEvent(
        UiKeyEvent keyEvent)
    {
        if (_console is null)
        {
            return;
        }

        if (keyEvent.Key == TextInputKey.Up &&
            NavigateHistoryUp())
        {
            keyEvent.Handled = true;
        }
        else if (keyEvent.Key == TextInputKey.Down &&
                 NavigateHistoryDown())
        {
            keyEvent.Handled = true;
        }
    }

    private bool NavigateHistoryUp()
    {
        var entries =
            _console?.History.Entries;

        if (entries is null ||
            entries.Count == 0)
        {
            return false;
        }

        if (_historyIndex < 0)
        {
            _historyDraft =
                _commandInput.Text;

            _historyIndex =
                entries.Count - 1;
        }
        else if (_historyIndex > 0)
        {
            _historyIndex--;
        }

        _commandInput.SetText(
            entries[_historyIndex]);

        return true;
    }

    private bool NavigateHistoryDown()
    {
        var entries =
            _console?.History.Entries;

        if (entries is null ||
            _historyIndex < 0)
        {
            return false;
        }

        if (_historyIndex < entries.Count - 1)
        {
            _historyIndex++;

            _commandInput.SetText(
                entries[_historyIndex]);

            return true;
        }

        _historyIndex = -1;

        _commandInput.SetText(
            _historyDraft);

        _historyDraft = string.Empty;

        return true;
    }

    protected override void OnUpdate(
    double deltaSeconds)
    {
        if (!_messagesDirty)
        {
            return;
        }

        _messagesDirty = false;

        RefreshMessages();
    }

    private sealed class ConsoleOutputView : UiWidget
    {
        private const float FontSize = 16.0f;
        private const float LineHeight = 22.0f;

        private readonly record struct RenderLine(
            string Text,
            UiColor Color);

        private IReadOnlyList<ConsoleMessage> _messages =
            Array.Empty<ConsoleMessage>();

        private readonly List<RenderLine> _lines = new();

        private readonly List<ConsoleMessage> _renderedMessages = new();

        private float _builtWidth = -1.0f;

        private bool _hasEmptyLine;

        private string _emptyMessage = "No messages yet.";

        private bool _layoutDirty = true;
        private bool _followTailPending;
        private int _followTailDelay;

        public void SetMessages(
     IReadOnlyList<ConsoleMessage> messages,
     string emptyMessage,
     bool forceScrollToBottom = false)
        {
            var wasAtBottom = IsAtBottom();

            var changed =
                !HaveSameMessages(_messages, messages) ||
                !string.Equals(
                    _emptyMessage,
                    emptyMessage,
                    StringComparison.Ordinal);

            _messages = messages;
            _emptyMessage = emptyMessage;

            if (changed)
            {
                _layoutDirty = true;
            }

            if (forceScrollToBottom || wasAtBottom)
            {
                _followTailPending = true;
                _followTailDelay = 1;
            }
        }

        private static bool HaveSameMessages(
    IReadOnlyList<ConsoleMessage> left,
    IReadOnlyList<ConsoleMessage> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (var i = 0; i < left.Count; i++)
            {
                if (!left[i].Equals(right[i]))
                {
                    return false;
                }
            }

            return true;
        }

        protected override Vector2 MeasureCore(
    UiLayoutContext context,
    Vector2 availableSize)
        {
            var maximumWidth =
                MathF.Max(
                    40.0f,
                    availableSize.X - 12.0f);

            var widthChanged =
                MathF.Abs(maximumWidth - _builtWidth) > 0.5f;

            if (_layoutDirty || widthChanged)
            {
                BuildLines(context, maximumWidth);
                _layoutDirty = false;
            }

            return new Vector2(
                availableSize.X,
                MathF.Max(
                    LineHeight + 8.0f,
                    _lines.Count * LineHeight + 8.0f));
        }

        protected override void OnUpdate(
            double deltaSeconds)
        {
            if (!_followTailPending)
            {
                return;
            }

            if (_followTailDelay > 0)
            {
                _followTailDelay--;
                return;
            }

            if (Parent is UiScrollView scrollView)
            {
                scrollView.ScrollToBottom();
            }

            _followTailPending = false;
        }

        protected override void OnRender(
            UiRenderContext context)
        {
            if (_layoutDirty ||
                _lines.Count == 0)
            {
                return;
            }

            var viewport =
                Parent is UiScrollView scrollView
                    ? scrollView.Bounds
                    : Bounds;

            var textTop =
                Bounds.Y + 4.0f;

            var first =
                Math.Max(
                    0,
                    (int)MathF.Floor(
                        (viewport.Y - textTop) /
                        LineHeight));

            var last =
                Math.Min(
                    _lines.Count,
                    (int)MathF.Ceiling(
                        (viewport.Bottom - textTop) /
                        LineHeight));

            for (var i = first; i < last; i++)
            {
                var line =
                    _lines[i];

                context.DrawText(
                    line.Text,
                    new Vector2(
                        Bounds.X + 6.0f,
                        textTop + i * LineHeight),
                    FontSize,
                    line.Color);
            }
        }

        private bool IsAtBottom()
        {
            if (Parent is not UiScrollView scrollView)
            {
                return true;
            }

            var maximum =
                MathF.Max(
                    0.0f,
                    DesiredSize.Y - scrollView.Bounds.Height);

            return maximum <=
                   scrollView.ScrollOffset.Y + 24.0f;
        }

        private void BuildLines(
    UiLayoutContext context,
    float maximumWidth)
        {
            var canAppend =
                !_hasEmptyLine &&
                MathF.Abs(_builtWidth - maximumWidth) <= 0.5f &&
                IsRenderedPrefixOfCurrent();

            if (!canAppend)
            {
                _lines.Clear();
                _renderedMessages.Clear();
                _hasEmptyLine = false;
            }

            if (_messages.Count == 0)
            {
                _lines.Clear();
                _renderedMessages.Clear();

                _lines.Add(
                    new RenderLine(
                        _emptyMessage,
                        new UiColor(135, 140, 150, 255)));

                _hasEmptyLine = true;
                _builtWidth = maximumWidth;

                return;
            }

            _hasEmptyLine = false;

            // Уже отрисованные сообщения не обрабатываем повторно.
            for (var i = _renderedMessages.Count;
                 i < _messages.Count;
                 i++)
            {
                var message = _messages[i];

                AppendMessage(
                    context,
                    message,
                    maximumWidth);

                _renderedMessages.Add(message);
            }

            _builtWidth = maximumWidth;
        }

        private void AppendMessage(
    UiLayoutContext context,
    ConsoleMessage message,
    float maximumWidth)
        {
            var prefix =
                $"{message.Timestamp.ToLocalTime():HH:mm:ss} " +
                $"[{message.Level}] ";

            var color =
                GetLevelColor(message.Level);

            var text =
                message.Message
                    .Replace("\r\n", "\n")
                    .Replace('\r', '\n');

            var logicalLines =
                text.Split('\n');

            var continuationPrefix =
                new string(' ', prefix.Length);

            for (var i = 0; i < logicalLines.Length; i++)
            {
                var firstPrefix =
                    i == 0
                        ? prefix
                        : continuationPrefix;

                AppendWrappedLine(
                    context,
                    logicalLines[i],
                    firstPrefix,
                    continuationPrefix,
                    maximumWidth,
                    color);
            }
        }
        private bool IsRenderedPrefixOfCurrent()
        {
            if (_renderedMessages.Count > _messages.Count)
            {
                return false;
            }

            for (var i = 0; i < _renderedMessages.Count; i++)
            {
                if (!_renderedMessages[i].Equals(_messages[i]))
                {
                    return false;
                }
            }

            return true;
        }
        private void AppendWrappedLine(
            UiLayoutContext context,
            string text,
            string firstPrefix,
            string continuationPrefix,
            float maximumWidth,
            UiColor color)
        {
            var current =
                firstPrefix;

            var words =
                text.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in words)
            {
                var candidate =
                    current == firstPrefix
                        ? current + word
                        : current + " " + word;

                if (current == firstPrefix ||
                    context.MeasureText(
                        candidate,
                        FontSize).X <= maximumWidth)
                {
                    current = candidate;
                    continue;
                }

                _lines.Add(
                    new RenderLine(
                        current.TrimEnd(),
                        color));

                current =
                    continuationPrefix + word;
            }

            _lines.Add(
                new RenderLine(
                    current.TrimEnd(),
                    color));
        }

        private static UiColor GetLevelColor(
            DiagnosticLevel level)
        {
            return level switch
            {
                DiagnosticLevel.Trace =>
                    new UiColor(135, 140, 150, 255),

                DiagnosticLevel.Debug =>
                    new UiColor(160, 175, 195, 255),

                DiagnosticLevel.Info =>
                    new UiColor(215, 218, 224, 255),

                DiagnosticLevel.Warning =>
                    new UiColor(235, 195, 105, 255),

                DiagnosticLevel.Error =>
                    new UiColor(240, 130, 140, 255),

                DiagnosticLevel.Critical =>
                    new UiColor(255, 90, 100, 255),

                _ =>
                    new UiColor(215, 218, 224, 255)
            };
        }
    }
}
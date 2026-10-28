
using Engine.Input;
using Engine.Tooling.Debugging;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.Editor.UI.Shell;

namespace Engine.Editor.UI;

internal sealed class EditorKeyboardShortcutHandler
{
    private static readonly InputBinding RenameEntityKey =
    new("Keyboard.F2");

    private static readonly InputBinding DeleteEntityKey =
        new("Keyboard.Delete");

    private static readonly InputBinding LeftAltKey =
        new("Keyboard.AltLeft");

    private static readonly InputBinding RightAltKey =
        new("Keyboard.AltRight");

    private static readonly InputBinding LeftShiftKey =
        new("Keyboard.ShiftLeft");

    private static readonly InputBinding RightShiftKey =
        new("Keyboard.ShiftRight");

    private static readonly InputBinding CopyKey =
        new("Keyboard.C");

    private static readonly InputBinding PasteKey =
        new("Keyboard.V");

    private static readonly InputBinding UndoKey =
        new("Keyboard.Z");

    private static readonly InputBinding RedoKey =
        new("Keyboard.Y");

    private static readonly InputBinding LeftControlKey =
        new("Keyboard.ControlLeft");

    private static readonly InputBinding RightControlKey =
        new("Keyboard.ControlRight");

    private readonly IInputBackend _input;
    private readonly UiSystem _ui;
    private readonly EditorMainShell _mainShell;
    private readonly IClipboardService? _clipboardService;
    private readonly DebugConsole? _debugConsole;

    private bool _undoShortcutWasDown;
    private bool _redoShortcutWasDown;
    private bool _copyShortcutWasDown;
    private bool _pasteShortcutWasDown;
    private bool _renameEntityShortcutWasDown;
    private bool _deleteEntityShortcutWasDown;
    public EditorKeyboardShortcutHandler(
        IInputBackend input,
        UiSystem ui,
        EditorMainShell mainShell,
        IClipboardService? clipboardService,
        DebugConsole? debugConsole)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(ui);
        ArgumentNullException.ThrowIfNull(mainShell);

        _input = input;
        _ui = ui;
        _mainShell = mainShell;
        _clipboardService = clipboardService;
        _debugConsole = debugConsole;
    }

    public void Update()
    {
        var controlDown =
            _input.GetValue(LeftControlKey) > 0.5f ||
            _input.GetValue(RightControlKey) > 0.5f;

        var copyDown =
            controlDown &&
            _input.GetValue(CopyKey) > 0.5f;

        var pasteDown =
            controlDown &&
            _input.GetValue(PasteKey) > 0.5f;

        var shiftDown =
            _input.GetValue(LeftShiftKey) > 0.5f ||
            _input.GetValue(RightShiftKey) > 0.5f;

        var altDown =
    _input.GetValue(LeftAltKey) > 0.5f ||
    _input.GetValue(RightAltKey) > 0.5f;

        var renameEntityDown =
            _input.GetValue(RenameEntityKey) > 0.5f;

        var deleteEntityDown =
            _input.GetValue(DeleteEntityKey) > 0.5f;

        var undoKeyDown =
            _input.GetValue(UndoKey) > 0.5f;

        var redoKeyDown =
            _input.GetValue(RedoKey) > 0.5f;

        var undoDown =
            controlDown &&
            !shiftDown &&
            undoKeyDown;

        var redoDown =
            controlDown &&
            (redoKeyDown ||
             (shiftDown && undoKeyDown));

        var focusedWidget = _ui.Focus.FocusedWidget;
        var focusedTextBox = focusedWidget as UiTextBox;

        var shortcutSuppressed =
            _ui.Focus.ActiveScope is not null ||
            focusedTextBox is not null;

        var consoleOutput = _mainShell.ConsoleOutputWidget;

        var canCopyConsoleSelection =
            _ui.Focus.ActiveScope is null &&
            focusedTextBox is null &&
            (ReferenceEquals(focusedWidget, consoleOutput) ||
             consoleOutput.IsHovered);

        // Ctrl+C: сначала выделение TextBox,
        // иначе выделенный текст консоли.
        if (copyDown &&
            !_copyShortcutWasDown &&
            _ui.Focus.ActiveScope is null)
        {
            var selectedText =
                focusedTextBox is not null
                    ? focusedTextBox.SelectedText
                    : canCopyConsoleSelection
                        ? _mainShell.ConsoleSelectedText
                        : string.Empty;

            if (selectedText.Length > 0)
            {
                if (_clipboardService is null)
                {
                    _debugConsole?.WriteError(
                        "Clipboard service is not configured.");
                }
                else
                {
                    try
                    {
                        _clipboardService.SetText(selectedText);
                    }
                    catch (Exception exception)
                    {
                        _debugConsole?.WriteException(
                            exception,
                            "Failed to copy selected text.");
                    }
                }
            }
        }

        // Ctrl+V: вставка только в активное поле ввода.
        if (pasteDown &&
            !_pasteShortcutWasDown &&
            _ui.Focus.ActiveScope is null &&
            focusedTextBox is not null)
        {
            if (_clipboardService is null)
            {
                _debugConsole?.WriteError(
                    "Clipboard service is not configured.");
            }
            else
            {
                try
                {
                    var clipboardText =
                        _clipboardService.GetText();

                    if (!string.IsNullOrEmpty(clipboardText))
                    {
                        focusedTextBox.InsertText(clipboardText);
                    }
                }
                catch (Exception exception)
                {
                    _debugConsole?.WriteException(
                        exception,
                        "Failed to paste clipboard text.");
                }
            }
        }

        // Не перехватываем Undo/Redo внутри полей ввода
        // и модальных окон.
        if (!shortcutSuppressed)
        {
            if (undoDown && !_undoShortcutWasDown)
            {
                _mainShell.Undo();
            }

            if (redoDown && !_redoShortcutWasDown)
            {
                _mainShell.Redo();
            }

            if (renameEntityDown &&
                !_renameEntityShortcutWasDown)
            {
                _mainShell.TryExecuteHierarchyShortcut(
                    RenameEntityKey,
                    controlDown,
                    shiftDown,
                    altDown);
            }

            if (deleteEntityDown &&
                !_deleteEntityShortcutWasDown)
            {
                _mainShell.TryExecuteHierarchyShortcut(
                    DeleteEntityKey,
                    controlDown,
                    shiftDown,
                    altDown);
            }
        }

        // Обрабатываем сочетание только при новом нажатии.
        _undoShortcutWasDown = undoDown;
        _redoShortcutWasDown = redoDown;
        _copyShortcutWasDown = copyDown;
        _pasteShortcutWasDown = pasteDown;
        _renameEntityShortcutWasDown = renameEntityDown;
        _deleteEntityShortcutWasDown = deleteEntityDown;
    }
}

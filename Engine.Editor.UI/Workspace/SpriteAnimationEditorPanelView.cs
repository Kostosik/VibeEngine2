using Engine.Editor.Assets;
using Engine.Editor.Documents;
using Engine.Editor.UI.Dialogs;
using Engine.Graphics.Commands;
using Engine.Graphics.Resources;
using Engine.Graphics2D.Sprites;
using Engine.UI.Controls;
using Engine.Core.Math;
using Engine.UI.Layout;
using System.Globalization;

namespace Engine.Editor.UI.Workspace;

public sealed class SpriteAnimationEditorPanelView : UiPanel
{
    private readonly SpriteAnimationDocument _document;
    private readonly EditorAssetPickerDialog? _assetPickerDialog;
    private readonly SpriteAnimationPreviewView? _previewView;
    private readonly UiLabel _summaryLabel;
    private readonly UiLabel _messageLabel;
    private readonly UiTextBox _durationTextBox;
    private readonly UiDropdown _loopDropdown;
    private readonly UiStackPanel _framesPanel;
    private readonly UiLabel _documentNameLabel;
    private readonly UiStackPanel _settingsRow;
    private readonly UiStackPanel _frameToolbar;
    private readonly UiScrollView _framesScroll;
    private readonly UiStackPanel _editorBody;

    private bool _bodyLayoutInitialized;
    private bool _bodyLayoutCompact;
    public SpriteAnimationEditorPanelView(
        SpriteAnimationDocument document,
        EditorAssetPickerDialog? assetPickerDialog = null)
        : this(
            document,
            null,
            null,
            assetPickerDialog)
    {
    }

    public SpriteAnimationEditorPanelView(
        SpriteAnimationDocument document,
        EditorContext? editor,
        ITextureManager? textureManager,
        EditorAssetPickerDialog? assetPickerDialog = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        _document = document;
        _assetPickerDialog = assetPickerDialog;

        _previewView = editor is null
            ? null
            : new SpriteAnimationPreviewView(
                document,
                editor,
                textureManager);

        Padding = new UiThickness(12.0f);
        Background = new UiColor(28, 28, 28, 255);

        var layout = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 8.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        layout.AddChild(new UiLabel("Sprite Animation")
        {
            Height = 30.0f,
            FontSize = 20.0f
        });

        _documentNameLabel = new UiLabel(document.DisplayName)
        {
            Height = 24.0f,
            FontSize = 13.0f,
            Color = new UiColor(170, 170, 170, 255)
        };

        layout.AddChild(_documentNameLabel);

        _summaryLabel = new UiLabel
        {
            Height = 28.0f,
            FontSize = 13.0f
        };

        layout.AddChild(_summaryLabel);

        _settingsRow = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 8.0f,
            Height = 34.0f
        };

        _settingsRow.AddChild(new UiLabel("Frame duration (seconds)")
        {
            Width = 175.0f,
            Height = 30.0f,
            FontSize = 13.0f,
            VerticalAlignment = UiVerticalAlignment.Center
        });

        _durationTextBox = new UiTextBox
        {
            Width = 110.0f,
            Height = 30.0f,
            FontSize = 13.0f,
            MaxLength = 32,
            Placeholder = "0.1"
        };

        _settingsRow.AddChild(_durationTextBox);

        _settingsRow.AddChild(new UiLabel("Playback")
        {
            Width = 65.0f,
            Height = 30.0f,
            FontSize = 13.0f,
            VerticalAlignment = UiVerticalAlignment.Center
        });

        _loopDropdown = new UiDropdown
        {
            Width = 100.0f,
            Height = 30.0f
        };

        _loopDropdown.AddOption("Loop");
        _loopDropdown.AddOption("Once");

        _settingsRow.AddChild(_loopDropdown);

        var applySettingsButton = new UiButton("Apply")
        {
            Width = 80.0f,
            Height = 30.0f
        };

        applySettingsButton.Clicked += ApplySettings;
        _settingsRow.AddChild(applySettingsButton);

        layout.AddChild(_settingsRow);

        _frameToolbar = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 8.0f,
            Height = 34.0f
        };

        var addFrameButton = new UiButton("Add Frame")
        {
            Width = 110.0f,
            Height = 30.0f
        };

        addFrameButton.Clicked += AddFrame;
        _frameToolbar.AddChild(addFrameButton);

        _frameToolbar.AddChild(new UiLabel(
            "Frame order determines playback order.")
        {
            Height = 30.0f,
            FontSize = 12.0f,
            Color = new UiColor(165, 165, 165, 255),
            VerticalAlignment = UiVerticalAlignment.Center
        });

        layout.AddChild(_frameToolbar);

        _messageLabel = new UiLabel
        {
            Height = 28.0f,
            FontSize = 12.0f,
            Color = new UiColor(220, 180, 100, 255)
        };

        layout.AddChild(_messageLabel);

        _framesScroll = new UiScrollView
        {
            Height = 500.0f,

            HorizontalScrolling = true,
            VerticalScrolling = true,

            ShowHorizontalScrollBar = true,
            ShowVerticalScrollBar = true,

            HorizontalAlignment =
                UiHorizontalAlignment.Stretch,

            VerticalAlignment =
                UiVerticalAlignment.Stretch
        };

        _framesPanel = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 6.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch
        };

        _framesScroll.SetContent(_framesPanel);

        _editorBody = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 12.0f,
            Height = 500.0f,

            HorizontalAlignment =
                UiHorizontalAlignment.Stretch,

            VerticalAlignment =
                UiVerticalAlignment.Stretch
        };

        _editorBody.AddChild(_framesScroll);

        if (_previewView is not null)
        {
            _previewView.Height = 320.0f;

            _previewView.VerticalAlignment =
                UiVerticalAlignment.Top;

            _editorBody.AddChild(_previewView);
        }

        layout.AddChild(_editorBody);

        // Прокручивается вся страница редактора,
        // а не только список кадров.
        var pageScroll = new UiScrollView
        {
            HorizontalScrolling = false,
            VerticalScrolling = true,

            ShowHorizontalScrollBar = false,
            ShowVerticalScrollBar = true,

            HorizontalAlignment =
                UiHorizontalAlignment.Stretch,

            VerticalAlignment =
                UiVerticalAlignment.Stretch
        };

        pageScroll.SetContent(layout);

        AddChild(pageScroll);

        _document.Changed += OnDocumentChanged;

        Refresh();
    }

    public SpriteAnimationDocument Document => _document;

    public void ReleaseResources()
    {
        _document.Changed -= OnDocumentChanged;
        _previewView?.ReleaseResources();
    }

    public void Refresh()
    {
        var definition = _document.Definition;

        _durationTextBox.SetText(
            Format(definition.FrameDurationSeconds));

        _loopDropdown.SetSelectedIndex(
            definition.Loop ? 0 : 1);

        _summaryLabel.Text =
            $"Frames: {definition.Frames.Count}" +
            $"    Duration: {Format(definition.FrameDurationSeconds)} s" +
            $"    Playback: {(definition.Loop ? "Loop" : "Once")}" +
            $"    Status: {(_document.IsDirty ? "Modified" : "Saved")}";

        _framesPanel.ClearChildren();

        if (definition.Frames.Count == 0)
        {
            _framesPanel.AddChild(new UiLabel(
                "No frames. Use Add Frame to select a texture.")
            {
                Height = 32.0f,
                FontSize = 13.0f,
                Color = new UiColor(170, 170, 170, 255)
            });

            return;
        }

        for (var i = 0; i < definition.Frames.Count; i++)
        {
            var index = i;
            var frame = definition.Frames[index];

            var row = new UiStackPanel
            {
                Orientation = UiOrientation.Horizontal,
                Spacing = 6.0f,
                Height = 34.0f,
                HorizontalAlignment = UiHorizontalAlignment.Stretch
            };

            row.AddChild(new UiLabel($"{index + 1}.")
            {
                Width = 32.0f,
                Height = 30.0f,
                FontSize = 13.0f,
                VerticalAlignment = UiVerticalAlignment.Center
            });

            var texturePicker = new UiPickerField
            {
                Width = 360.0f,
                Height = 30.0f,
                FontSize = 12.0f,
                Placeholder = "Select texture"
            };

            texturePicker.SelectedText = frame.TexturePath;

            texturePicker.SelectionRequested += () =>
                ShowTexturePicker(
                    "Select frame texture",
                    path => ReplaceFrame(index, path));

            row.AddChild(texturePicker);

            var upButton = new UiButton("Up")
            {
                Width = 48.0f,
                Height = 30.0f,
                FontSize = 12.0f,
                Enabled = index > 0
            };

            upButton.Clicked += () => MoveFrame(index, index - 1);
            row.AddChild(upButton);

            var downButton = new UiButton("Down")
            {
                Width = 58.0f,
                Height = 30.0f,
                FontSize = 12.0f,
                Enabled = index < definition.Frames.Count - 1
            };

            downButton.Clicked += () => MoveFrame(index, index + 1);
            row.AddChild(downButton);

            var removeButton = new UiButton("Remove")
            {
                Width = 76.0f,
                Height = 30.0f,
                FontSize = 12.0f
            };

            removeButton.Clicked += () => RemoveFrame(index);
            row.AddChild(removeButton);

            _framesPanel.AddChild(row);
        }
    }

    private void ApplySettings()
    {
        try
        {
            if (!float.TryParse(
                    _durationTextBox.Text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var duration) ||
                !float.IsFinite(duration) ||
                duration <= 0.0f)
            {
                throw new InvalidDataException(
                    "Frame duration must be a finite number greater than zero.");
            }

            var current = _document.Definition;
            var loop = _loopDropdown.SelectedText == "Loop";

            _document.UpdateDefinition(
                new SpriteAnimationDefinition(
                    current.Frames,
                    duration,
                    loop));

            _messageLabel.Text = "Animation settings updated.";
        }
        catch (Exception exception)
        {
            _messageLabel.Text = exception.Message;
        }
    }

    private void AddFrame()
    {
        ShowTexturePicker(
            "Select texture for new frame",
            AddFrameFromPath);
    }

    private void ShowTexturePicker(
        string title,
        Action<string> selected)
    {
        if (_assetPickerDialog is null)
        {
            _messageLabel.Text =
                "Asset Picker is not available.";

            return;
        }

        var opened = _assetPickerDialog.Show(
            title,
            IsTextureAsset,
            selected);

        if (!opened)
        {
            _messageLabel.Text =
                "Open a game project to select textures.";
        }
    }

    private void AddFrameFromPath(string path)
    {
        var current = _document.Definition;

        var frames = current.Frames
            .Append(new SpriteAnimationFrameDefinition(path))
            .ToArray();

        UpdateFrames(frames);

        _messageLabel.Text = "Frame added.";
    }

    private void ReplaceFrame(int index, string path)
    {
        var current = _document.Definition;

        if (index < 0 || index >= current.Frames.Count)
        {
            return;
        }

        var frames = current.Frames.ToArray();
        frames[index] = new SpriteAnimationFrameDefinition(path);

        UpdateFrames(frames);

        _messageLabel.Text = "Frame texture updated.";
    }

    private void RemoveFrame(int index)
    {
        var current = _document.Definition;

        if (index < 0 || index >= current.Frames.Count)
        {
            return;
        }

        var frames = current.Frames
            .Where((_, i) => i != index)
            .ToArray();

        UpdateFrames(frames);

        _messageLabel.Text = "Frame removed.";
    }

    private void MoveFrame(int fromIndex, int toIndex)
    {
        var current = _document.Definition;

        if (fromIndex < 0 ||
            fromIndex >= current.Frames.Count ||
            toIndex < 0 ||
            toIndex >= current.Frames.Count ||
            fromIndex == toIndex)
        {
            return;
        }

        var frames = current.Frames.ToArray();

        (frames[fromIndex], frames[toIndex]) =
            (frames[toIndex], frames[fromIndex]);

        UpdateFrames(frames);

        _messageLabel.Text = "Frame order updated.";
    }

    protected override Vector2 MeasureCore(
    UiLayoutContext context,
    Vector2 availableSize)
    {
        var contentWidth = MathF.Max(
            0.0f,
            availableSize.X -
            Padding.Left -
            Padding.Right);

        var compactBody =
            _previewView is not null &&
            contentWidth < 760.0f;

        var compactSettings =
            contentWidth < 580.0f;

        var compactToolbar =
            contentWidth < 440.0f;

        // На узком экране настройки располагаются
        // вертикально, чтобы не выходить за границы.
        _settingsRow.Orientation =
            compactSettings
                ? UiOrientation.Vertical
                : UiOrientation.Horizontal;

        _settingsRow.Height =
            compactSettings
                ? null
                : 34.0f;

        // Панель добавления кадров тоже адаптируется.
        _frameToolbar.Orientation =
            compactToolbar
                ? UiOrientation.Vertical
                : UiOrientation.Horizontal;

        _frameToolbar.Height =
            compactToolbar
                ? null
                : 34.0f;

        // Ограничиваем ширину текстовых заголовков.
        _documentNameLabel.Width = contentWidth;
        _summaryLabel.Width = contentWidth;
        _messageLabel.Width = contentWidth;

        // Когда превью становится узким, оно переносится
        // над списком кадров.
        SetBodyLayoutOrder(compactBody);

        _framesScroll.HorizontalScrolling = true;
        _framesScroll.ShowHorizontalScrollBar = true;

        if (compactBody)
        {
            _editorBody.Orientation =
                UiOrientation.Vertical;

            _editorBody.Height = null;

            _framesScroll.Width = contentWidth;
            _framesScroll.Height = 280.0f;

            _previewView!.Width = contentWidth;
            _previewView.Height = 320.0f;
        }
        else
        {
            _editorBody.Orientation =
                UiOrientation.Horizontal;

            _editorBody.Height = 500.0f;
            _framesScroll.Height = 500.0f;

            if (_previewView is null)
            {
                _framesScroll.Width = contentWidth;
            }
            else
            {
                var framesWidth = Math.Clamp(
                    contentWidth * 0.62f,
                    360.0f,
                    620.0f);

                var previewWidth = Math.Clamp(
                    contentWidth - framesWidth - 12.0f,
                    220.0f,
                    380.0f);

                _framesScroll.Width = framesWidth;

                _previewView.Width = previewWidth;
                _previewView.Height = 320.0f;
            }
        }

        return base.MeasureCore(
            context,
            availableSize);
    }

    private void SetBodyLayoutOrder(bool compact)
    {
        if (_bodyLayoutInitialized &&
            _bodyLayoutCompact == compact)
        {
            return;
        }

        if (_previewView is not null)
        {
            _editorBody.RemoveChild(_framesScroll);
            _editorBody.RemoveChild(_previewView);

            if (compact)
            {
                // На узком экране превью должно быть первым,
                // чтобы оно оставалось доступным.
                _editorBody.AddChild(_previewView);
                _editorBody.AddChild(_framesScroll);
            }
            else
            {
                // На широком экране список слева,
                // превью справа.
                _editorBody.AddChild(_framesScroll);
                _editorBody.AddChild(_previewView);
            }
        }

        _bodyLayoutCompact = compact;
        _bodyLayoutInitialized = true;
    }

    private void UpdateFrames(
        IReadOnlyList<SpriteAnimationFrameDefinition> frames)
    {
        var current = _document.Definition;

        _document.UpdateDefinition(
            new SpriteAnimationDefinition(
                frames,
                current.FrameDurationSeconds,
                current.Loop));
    }

    private static bool IsTextureAsset(EditorAssetEntry entry)
    {
        if (entry.IsDirectory)
        {
            return false;
        }

        var extension = Path.GetExtension(entry.Name);

        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".tga", StringComparison.OrdinalIgnoreCase);
    }

    private void OnDocumentChanged()
    {
        Refresh();
    }

    private static string Format(float value) =>
        value.ToString("G", CultureInfo.InvariantCulture);
}
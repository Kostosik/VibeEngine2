using Engine.Animation;
using Engine.Core.Math;
using Engine.Editor.Documents;
using Engine.Graphics.Commands;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Layout;
using System.Globalization;

namespace Engine.Editor.UI.Workspace;

public sealed class AnimationClipEditorPanelView : UiPanel
{
    private readonly AnimationClipDocument _document;
    private readonly UiLabel _summaryLabel;
    private readonly UiLabel _messageLabel;
    private readonly UiTextBox _durationTextBox;
    private readonly UiTextBox _trackIdTextBox;
    private readonly UiDropdown _trackTypeDropdown;
    private readonly UiStackPanel _tracksPanel;

    public AnimationClipEditorPanelView(
        AnimationClipDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _document = document;

        Padding = new UiThickness(12.0f);
        Background = new UiColor(28, 28, 28, 255);

        var layout = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 8.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        layout.AddChild(new UiLabel("Animation Clip")
        {
            Height = 30.0f,
            FontSize = 20.0f
        });

        layout.AddChild(new UiLabel(document.DisplayName)
        {
            Height = 24.0f,
            FontSize = 13.0f,
            Color = new UiColor(170, 170, 170, 255)
        });

        _summaryLabel = new UiLabel
        {
            Height = 28.0f,
            FontSize = 13.0f
        };

        layout.AddChild(_summaryLabel);

        // Duration controls.
        var durationRow = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 8.0f,
            Height = 34.0f
        };

        durationRow.AddChild(new UiLabel("Duration (seconds)")
        {
            Width = 150.0f,
            Height = 30.0f,
            FontSize = 13.0f,
            VerticalAlignment = UiVerticalAlignment.Center
        });

        _durationTextBox = new UiTextBox
        {
            Width = 130.0f,
            Height = 30.0f,
            FontSize = 13.0f,
            MaxLength = 32,
            Placeholder = "Duration"
        };

        var applyDurationButton = new UiButton("Apply Duration")
        {
            Width = 130.0f,
            Height = 30.0f
        };

        applyDurationButton.Clicked += ApplyDuration;

        durationRow.AddChild(_durationTextBox);
        durationRow.AddChild(applyDurationButton);
        layout.AddChild(durationRow);

        // Track creation controls.
        var trackRow = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 8.0f,
            Height = 34.0f
        };

        _trackIdTextBox = new UiTextBox
        {
            Width = 180.0f,
            Height = 30.0f,
            FontSize = 13.0f,
            MaxLength = 128,
            Placeholder = "Track ID, e.g. opacity"
        };

        _trackTypeDropdown = new UiDropdown
        {
            Width = 130.0f,
            Height = 30.0f
        };

        _trackTypeDropdown.AddOption("Float");
        _trackTypeDropdown.AddOption("Vector2");
        _trackTypeDropdown.AddOption("Vector3");
        _trackTypeDropdown.AddOption("Int");
        _trackTypeDropdown.SetSelectedIndex(0);

        var addTrackButton = new UiButton("Add Track")
        {
            Width = 100.0f,
            Height = 30.0f
        };

        addTrackButton.Clicked += AddTrack;

        trackRow.AddChild(_trackIdTextBox);
        trackRow.AddChild(_trackTypeDropdown);
        trackRow.AddChild(addTrackButton);

        layout.AddChild(trackRow);

        _messageLabel = new UiLabel
        {
            Height = 26.0f,
            FontSize = 12.0f,
            Color = new UiColor(220, 180, 100, 255)
        };

        layout.AddChild(_messageLabel);

        var scroll = new UiScrollView
        {
            Height = 500.0f,
            HorizontalScrolling = false,
            VerticalScrolling = true,
            ShowVerticalScrollBar = true,
            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        _tracksPanel = new UiStackPanel
        {
            Orientation = UiOrientation.Vertical,
            Spacing = 8.0f,
            HorizontalAlignment = UiHorizontalAlignment.Stretch
        };

        scroll.SetContent(_tracksPanel);
        layout.AddChild(scroll);

        AddChild(layout);

        _document.Changed += OnDocumentChanged;

        Refresh();
    }

    public AnimationClipDocument Document => _document;

    public void ReleaseResources()
    {
        _document.Changed -= OnDocumentChanged;
    }

    public void Refresh()
    {
        var clip = _document.Clip;

        _durationTextBox.SetText(
            FormatTime(clip.DurationSeconds));

        _summaryLabel.Text =
            $"Duration: {FormatTime(clip.DurationSeconds)} s" +
            $"    Tracks: {clip.TrackCount}" +
            $"    Status: {(_document.IsDirty ? "Modified" : "Saved")}";

        _tracksPanel.ClearChildren();

        if (clip.TrackCount == 0)
        {
            _tracksPanel.AddChild(new UiLabel(
                "No animation tracks. Use Add Track to create one.")
            {
                Height = 30.0f,
                FontSize = 13.0f,
                Color = new UiColor(170, 170, 170, 255)
            });

            return;
        }

        foreach (var pair in clip.Tracks.OrderBy(
                     static pair => pair.Key.Value,
                     StringComparer.Ordinal))
        {
            var trackId = pair.Key;
            var track = pair.Value;

            var section = new UiStackPanel
            {
                Orientation = UiOrientation.Vertical,
                Spacing = 3.0f,
                HorizontalAlignment = UiHorizontalAlignment.Stretch
            };

            var headerRow = new UiStackPanel
            {
                Orientation = UiOrientation.Horizontal,
                Spacing = 8.0f,
                Height = 30.0f
            };

            headerRow.AddChild(new UiLabel(
                $"{trackId.Value}  •  {track.ValueType.Name}")
            {
                Width = 360.0f,
                Height = 28.0f,
                FontSize = 14.0f,
                Color = new UiColor(220, 220, 220, 255)
            });

            var removeButton = new UiButton("Remove Track")
            {
                Width = 120.0f,
                Height = 28.0f,
                FontSize = 12.0f
            };

            removeButton.Clicked += () => RemoveTrack(trackId);

            headerRow.AddChild(removeButton);
            section.AddChild(headerRow);

            section.AddChild(new UiLabel(
                $"Interpolator: {track.InterpolatorType.Name}" +
                $"    Keyframes: {track.KeyframeCount}")
            {
                Height = 22.0f,
                FontSize = 12.0f,
                Color = new UiColor(165, 165, 165, 255)
            });

            for (var i = 0; i < track.KeyframeCount; i++)
            {
                var time = track.GetKeyframeTimeSeconds(i);
                var value = track.GetKeyframeValue(i);

                section.AddChild(new UiLabel(
                    $"    {i + 1}.  {FormatTime(time)} s    →    {FormatValue(value)}")
                {
                    Height = 22.0f,
                    FontSize = 12.0f
                });
            }

            _tracksPanel.AddChild(section);
        }
    }

    private void ApplyDuration()
    {
        try
        {
            if (!double.TryParse(
                    _durationTextBox.Text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var duration) ||
                !double.IsFinite(duration) ||
                duration <= 0.0)
            {
                throw new InvalidDataException(
                    "Duration must be a finite number greater than zero.");
            }

            var updatedClip = CloneClip(
                _document.Clip,
                duration);

            _document.UpdateClip(updatedClip);

            _messageLabel.Text = "Animation duration updated.";
        }
        catch (Exception exception)
        {
            _messageLabel.Text = exception.Message;
        }
    }

    private void AddTrack()
    {
        try
        {
            var id = new AnimationTrackId(_trackIdTextBox.Text);
            var current = _document.Clip;

            if (current.Tracks.ContainsKey(id))
            {
                throw new InvalidOperationException(
                    $"Track '{id.Value}' already exists.");
            }

            var duration = current.DurationSeconds;
            var updatedClip = CloneClip(current, duration);

            switch (_trackTypeDropdown.SelectedText)
            {
                case "Float":
                    updatedClip.AddTrack(
                        id,
                        new AnimationTrackBuilder<float>(
                            new FloatLinearAnimationInterpolator(),
                            new[]
                            {
                            new AnimationKeyframe<float>(0.0, 0.0f),
                            new AnimationKeyframe<float>(duration, 1.0f)
                            }).Build());
                    break;

                case "Vector2":
                    updatedClip.AddTrack(
                        id,
                        new AnimationTrackBuilder<Vector2>(
                            new Vector2LinearAnimationInterpolator(),
                            new[]
                            {
                            new AnimationKeyframe<Vector2>(
                                0.0, new Vector2(0.0f, 0.0f)),
                            new AnimationKeyframe<Vector2>(
                                duration, new Vector2(1.0f, 1.0f))
                            }).Build());
                    break;

                case "Vector3":
                    updatedClip.AddTrack(
                        id,
                        new AnimationTrackBuilder<Vector3>(
                            new Vector3LinearAnimationInterpolator(),
                            new[]
                            {
                            new AnimationKeyframe<Vector3>(
                                0.0, new Vector3(0.0f, 0.0f, 0.0f)),
                            new AnimationKeyframe<Vector3>(
                                duration, new Vector3(1.0f, 1.0f, 1.0f))
                            }).Build());
                    break;

                case "Int":
                    updatedClip.AddTrack(
                        id,
                        new AnimationTrackBuilder<int>(
                            new StepAnimationInterpolator<int>(),
                            new[]
                            {
                            new AnimationKeyframe<int>(0.0, 0),
                            new AnimationKeyframe<int>(duration, 1)
                            }).Build());
                    break;

                default:
                    throw new InvalidOperationException(
                        "Select a supported track type.");
            }

            _document.UpdateClip(updatedClip);
            _trackIdTextBox.SetText(string.Empty);
            _messageLabel.Text = $"Track '{id.Value}' added.";
        }
        catch (Exception exception)
        {
            _messageLabel.Text = exception.Message;
        }
    }

    private void RemoveTrack(AnimationTrackId id)
    {
        try
        {
            var updatedClip = CloneClip(
                _document.Clip,
                _document.Clip.DurationSeconds,
                id);

            _document.UpdateClip(updatedClip);
            _messageLabel.Text = $"Track '{id.Value}' removed.";
        }
        catch (Exception exception)
        {
            _messageLabel.Text = exception.Message;
        }
    }

    private static AnimationClip CloneClip(
        AnimationClip source,
        double durationSeconds,
        AnimationTrackId? excludedTrack = null)
    {
        var result = new AnimationClip(durationSeconds);

        foreach (var pair in source.Tracks)
        {
            if (excludedTrack.HasValue &&
                pair.Key.Equals(excludedTrack.Value))
            {
                continue;
            }

            pair.Value
                .CreateBuilder()
                .AddTo(result, pair.Key);
        }

        return result;
    }

    private void OnDocumentChanged()
    {
        Refresh();
    }

    private static string FormatTime(double value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "null",

            float number =>
                number.ToString("G", CultureInfo.InvariantCulture),

            double number =>
                number.ToString("G", CultureInfo.InvariantCulture),

            int number =>
                number.ToString(CultureInfo.InvariantCulture),

            Vector2 vector =>
                $"({FormatValue(vector.X)}, {FormatValue(vector.Y)})",

            Vector3 vector =>
                $"({FormatValue(vector.X)}, {FormatValue(vector.Y)}, " +
                $"{FormatValue(vector.Z)})",

            _ => Convert.ToString(value, CultureInfo.InvariantCulture)
                 ?? value.ToString()
                 ?? string.Empty
        };
    }
}
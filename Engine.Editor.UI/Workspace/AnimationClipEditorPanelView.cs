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
    private readonly UiLabel _documentNameLabel;
    private readonly UiStackPanel _durationRow;
    private readonly UiStackPanel _trackRow;
    private readonly UiScrollView _tracksScroll;

    private bool _compactLayout;
    private readonly AnimationClipDocument _document;
    private readonly UiLabel _summaryLabel;
    private readonly UiLabel _messageLabel;
    private readonly UiTextBox _durationTextBox;
    private readonly UiTextBox _trackIdTextBox;
    private readonly UiDropdown _trackTypeDropdown;
    private readonly UiStackPanel _tracksPanel;
    private readonly AnimationTimelineView _timelineView;
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

        // Duration controls.
        _durationRow = new UiStackPanel
        {
            Orientation = UiOrientation.Horizontal,
            Spacing = 8.0f,
            Height = 34.0f
        };

        _durationRow.AddChild(new UiLabel("Duration (seconds)")
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

        _durationRow.AddChild(_durationTextBox);
        _durationRow.AddChild(applyDurationButton);
        layout.AddChild(_durationRow);

        // Track creation controls.
        _trackRow = new UiStackPanel
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

        _trackRow.AddChild(_trackIdTextBox);
        _trackRow.AddChild(_trackTypeDropdown);
        _trackRow.AddChild(addTrackButton);

        layout.AddChild(_trackRow);

        _messageLabel = new UiLabel
        {
            Height = 26.0f,
            FontSize = 12.0f,
            Color = new UiColor(220, 180, 100, 255)
        };

        layout.AddChild(_messageLabel);

        _timelineView = new AnimationTimelineView(document)
        {
            Height = 170.0f
        };

        _timelineView.MessageChanged += OnTimelineMessageChanged;

        layout.AddChild(_timelineView);

        _tracksScroll = new UiScrollView
        {
            Height = 500.0f,

            HorizontalScrolling = true,
            VerticalScrolling = true,

            ShowHorizontalScrollBar = true,
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

        _tracksScroll.SetContent(_tracksPanel);
        layout.AddChild(_tracksScroll);

        var pageScroll = new UiScrollView
        {
            HorizontalScrolling = false,
            VerticalScrolling = true,

            ShowHorizontalScrollBar = false,
            ShowVerticalScrollBar = true,

            HorizontalAlignment = UiHorizontalAlignment.Stretch,
            VerticalAlignment = UiVerticalAlignment.Stretch
        };

        pageScroll.SetContent(layout);

        AddChild(pageScroll);

        _document.Changed += OnDocumentChanged;

        Refresh();
    }

    public AnimationClipDocument Document => _document;

    protected override Vector2 MeasureCore(
    UiLayoutContext context,
    Vector2 availableSize)
    {
        var contentWidth = MathF.Max(
            0.0f,
            availableSize.X -
            Padding.Left -
            Padding.Right);

        var compact = contentWidth < 720.0f;

        _compactLayout = compact;

        _documentNameLabel.Width = contentWidth;
        _summaryLabel.Width = contentWidth;
        _messageLabel.Width = contentWidth;

        // Строка изменения длительности.
        _durationRow.Orientation =
            compact
                ? UiOrientation.Vertical
                : UiOrientation.Horizontal;

        _durationRow.Height =
            compact
                ? null
                : 34.0f;

        _durationRow.Children[0].Width =
            compact ? contentWidth : 150.0f;

        _durationRow.Children[1].Width =
            compact ? contentWidth : 130.0f;

        _durationRow.Children[2].Width =
            compact ? contentWidth : 130.0f;

        // Строка создания трека.
        _trackRow.Orientation =
            compact
                ? UiOrientation.Vertical
                : UiOrientation.Horizontal;

        _trackRow.Height =
            compact
                ? null
                : 34.0f;

        _trackRow.Children[0].Width =
            compact ? contentWidth : 180.0f;

        _trackRow.Children[1].Width =
            compact ? contentWidth : 130.0f;

        _trackRow.Children[2].Width =
            compact ? contentWidth : 100.0f;

        // Сохраняем ширину временной шкалы
        // по доступной ширине родительской панели.
        _timelineView.Width = contentWidth;
        _timelineView.Height = compact ? 140.0f : 170.0f;

        // Список треков занимает доступную ширину.
        _tracksScroll.Width = contentWidth;
        _tracksScroll.Height = 500.0f;

        foreach (var section in
                 _tracksPanel.Children.OfType<UiStackPanel>())
        {
            var headerRow = section.Children
                .OfType<UiStackPanel>()
                .FirstOrDefault();

            if (headerRow is null)
            {
                continue;
            }

            // Заголовок трека и его кнопки.
            headerRow.Orientation =
                compact
                    ? UiOrientation.Vertical
                    : UiOrientation.Horizontal;

            headerRow.Height =
                compact
                    ? null
                    : 30.0f;

            if (headerRow.Children.Count >= 3)
            {
                headerRow.Children[0].Width =
                    compact ? contentWidth : 280.0f;

                headerRow.Children[1].Width = 90.0f;
                headerRow.Children[2].Width = 110.0f;
            }

            // Строки ключевых кадров.
            foreach (var row in section.Children
                         .OfType<UiStackPanel>()
                         .Where(row =>
                             !ReferenceEquals(row, headerRow)))
            {
                if (row.Children.Count != 5)
                {
                    continue;
                }

                row.Orientation =
                    compact
                        ? UiOrientation.Vertical
                        : UiOrientation.Horizontal;

                row.Height =
                    compact
                        ? null
                        : 30.0f;

                row.Children[0].Width =
                    compact ? contentWidth : 24.0f;

                row.Children[1].Width =
                    compact ? contentWidth : 100.0f;

                row.Children[2].Width =
                    compact ? contentWidth : 240.0f;

                row.Children[3].Width =
                    compact ? contentWidth : 65.0f;

                row.Children[4].Width =
                    compact ? contentWidth : 75.0f;
            }
        }

        return base.MeasureCore(
            context,
            availableSize);
    }

    private void OnTimelineMessageChanged(string message)
    {
        _messageLabel.Text = message;
    }

    public void ReleaseResources()
    {
        _timelineView.MessageChanged -= OnTimelineMessageChanged;
        _timelineView.ReleaseResources();
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
                Width = 280.0f,
                Height = 28.0f,
                FontSize = 14.0f,
                Color = new UiColor(220, 220, 220, 255)
            });

            var addKeyButton = new UiButton("Add Key")
            {
                Width = 90.0f,
                Height = 28.0f,
                FontSize = 12.0f
            };

            addKeyButton.Clicked += () => AddKeyframe(trackId);
            headerRow.AddChild(addKeyButton);

            var removeTrackButton = new UiButton("Remove Track")
            {
                Width = 110.0f,
                Height = 28.0f,
                FontSize = 12.0f
            };

            removeTrackButton.Clicked += () => RemoveTrack(trackId);
            headerRow.AddChild(removeTrackButton);

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
                var keyframeIndex = i;

                var timeBox = new UiTextBox(
                    FormatTime(track.GetKeyframeTimeSeconds(i)))
                {
                    Width = 100.0f,
                    Height = 28.0f,
                    FontSize = 12.0f,
                    MaxLength = 32,
                    Placeholder = "Time"
                };

                var valueBox = new UiTextBox(
                    FormatValue(track.GetKeyframeValue(i)))
                {
                    Width = 240.0f,
                    Height = 28.0f,
                    FontSize = 12.0f,
                    MaxLength = 128,
                    Placeholder = "Value"
                };

                var applyButton = new UiButton("Apply")
                {
                    Width = 65.0f,
                    Height = 28.0f,
                    FontSize = 12.0f
                };

                applyButton.Clicked += () =>
                    ApplyKeyframe(
                        trackId,
                        keyframeIndex,
                        timeBox.Text,
                        valueBox.Text);

                var removeButton = new UiButton("Remove")
                {
                    Width = 75.0f,
                    Height = 28.0f,
                    FontSize = 12.0f
                };

                removeButton.Clicked += () =>
                    RemoveKeyframe(trackId, keyframeIndex);

                var keyframeRow = new UiStackPanel
                {
                    Orientation = UiOrientation.Horizontal,
                    Spacing = 6.0f,
                    Height = 30.0f
                };

                keyframeRow.AddChild(new UiLabel($"{i + 1}.")
                {
                    Width = 24.0f,
                    Height = 28.0f,
                    FontSize = 12.0f,
                    VerticalAlignment = UiVerticalAlignment.Center
                });

                keyframeRow.AddChild(timeBox);
                keyframeRow.AddChild(valueBox);
                keyframeRow.AddChild(applyButton);
                keyframeRow.AddChild(removeButton);

                section.AddChild(keyframeRow);
            }

            _tracksPanel.AddChild(section);
        }
    }

    private void ApplyKeyframe(
    AnimationTrackId trackId,
    int index,
    string timeText,
    string valueText)
    {
        try
        {
            if (!double.TryParse(
                    timeText,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var time) ||
                !double.IsFinite(time) ||
                time < 0.0 ||
                time > _document.Clip.DurationSeconds)
            {
                throw new InvalidDataException(
                    "Keyframe time must be between zero and the clip duration.");
            }

            var clip = _document.Clip;
            var track = clip.Tracks[trackId];
            var frames = ReadKeyframes(track);

            if (index < 0 || index >= frames.Count)
            {
                return;
            }

            var value = ParseValue(track.ValueType, valueText);

            frames[index] = (time, value);

            ReplaceTrack(clip, trackId, frames);

            _messageLabel.Text = "Keyframe updated.";
        }
        catch (Exception exception)
        {
            _messageLabel.Text = exception.Message;
        }
    }

    private void AddKeyframe(AnimationTrackId trackId)
    {
        try
        {
            var clip = _document.Clip;
            var track = clip.Tracks[trackId];

            if (track.KeyframeCount == 0)
            {
                throw new InvalidOperationException(
                    "Cannot add a keyframe to an empty track.");
            }

            double left;
            double right;

            if (track.KeyframeCount == 1)
            {
                var existingTime = track.GetKeyframeTimeSeconds(0);

                if (existingTime < clip.DurationSeconds)
                {
                    left = existingTime;
                    right = clip.DurationSeconds;
                }
                else
                {
                    left = 0.0;
                    right = existingTime;
                }
            }
            else
            {
                left = track.GetKeyframeTimeSeconds(0);
                right = track.GetKeyframeTimeSeconds(1);

                var largestGap = right - left;

                for (var i = 1; i < track.KeyframeCount - 1; i++)
                {
                    var candidateLeft =
                        track.GetKeyframeTimeSeconds(i);

                    var candidateRight =
                        track.GetKeyframeTimeSeconds(i + 1);

                    var gap = candidateRight - candidateLeft;

                    if (gap > largestGap)
                    {
                        largestGap = gap;
                        left = candidateLeft;
                        right = candidateRight;
                    }
                }
            }

            var newTime = left + (right - left) / 2.0;

            if (newTime <= left || newTime >= right)
            {
                throw new InvalidOperationException(
                    "There is no available interval for another keyframe.");
            }

            var frames = ReadKeyframes(track);

            // Берём значение из текущей анимации в выбранном времени.
            var sampledValue = track.SampleObject(newTime);

            frames.Add((newTime, sampledValue));

            ReplaceTrack(clip, trackId, frames);

            _messageLabel.Text =
                $"Keyframe added at {FormatTime(newTime)} seconds.";
        }
        catch (Exception exception)
        {
            _messageLabel.Text = exception.Message;
        }
    }

    private void RemoveKeyframe(
        AnimationTrackId trackId,
        int index)
    {
        try
        {
            var clip = _document.Clip;
            var track = clip.Tracks[trackId];

            if (track.KeyframeCount <= 1)
            {
                throw new InvalidOperationException(
                    "A track must contain at least one keyframe. Remove the track instead.");
            }

            var frames = ReadKeyframes(track);

            if (index < 0 || index >= frames.Count)
            {
                return;
            }

            frames.RemoveAt(index);

            ReplaceTrack(clip, trackId, frames);

            _messageLabel.Text = "Keyframe removed.";
        }
        catch (Exception exception)
        {
            _messageLabel.Text = exception.Message;
        }
    }

    private static List<(double TimeSeconds, object? Value)> ReadKeyframes(
    IAnimationTrack track)
    {
        var result =
            new List<(double TimeSeconds, object? Value)>(
                track.KeyframeCount);

        for (var i = 0; i < track.KeyframeCount; i++)
        {
            result.Add((
                track.GetKeyframeTimeSeconds(i),
                track.GetKeyframeValue(i)));
        }

        return result;
    }

    private void ReplaceTrack(
        AnimationClip source,
        AnimationTrackId trackId,
        List<(double TimeSeconds, object? Value)> frames)
    {
        frames.Sort(
            static (left, right) =>
                left.TimeSeconds.CompareTo(right.TimeSeconds));

        for (var i = 0; i < frames.Count; i++)
        {
            var time = frames[i].TimeSeconds;

            if (!double.IsFinite(time) ||
                time < 0.0 ||
                time > source.DurationSeconds)
            {
                throw new InvalidDataException(
                    "Every keyframe must fit within the clip duration.");
            }

            if (i > 0 && frames[i - 1].TimeSeconds >= time)
            {
                throw new InvalidDataException(
                    "Keyframe times must be unique.");
            }
        }

        if (frames.Count == 0)
        {
            throw new InvalidOperationException(
                "A track must contain at least one keyframe.");
        }

        var updatedClip = new AnimationClip(source.DurationSeconds);

        foreach (var pair in source.Tracks)
        {
            if (pair.Key.Equals(trackId))
            {
                AddEditedTrack(
                    updatedClip,
                    trackId,
                    pair.Value,
                    frames);
            }
            else
            {
                pair.Value.CreateBuilder().AddTo(
                    updatedClip,
                    pair.Key);
            }
        }

        _document.UpdateClip(updatedClip);
    }

    private static void AddEditedTrack(
        AnimationClip target,
        AnimationTrackId id,
        IAnimationTrack sourceTrack,
        IReadOnlyList<(double TimeSeconds, object? Value)> frames)
    {
        if (sourceTrack.ValueType == typeof(float) &&
            sourceTrack.InterpolatorType ==
            typeof(FloatLinearAnimationInterpolator))
        {
            target.AddTrack(
                id,
                BuildTrack<float>(
                    new FloatLinearAnimationInterpolator(),
                    frames));
            return;
        }

        if (sourceTrack.ValueType == typeof(Vector2) &&
            sourceTrack.InterpolatorType ==
            typeof(Vector2LinearAnimationInterpolator))
        {
            target.AddTrack(
                id,
                BuildTrack<Vector2>(
                    new Vector2LinearAnimationInterpolator(),
                    frames));
            return;
        }

        if (sourceTrack.ValueType == typeof(Vector3) &&
            sourceTrack.InterpolatorType ==
            typeof(Vector3LinearAnimationInterpolator))
        {
            target.AddTrack(
                id,
                BuildTrack<Vector3>(
                    new Vector3LinearAnimationInterpolator(),
                    frames));
            return;
        }

        if (sourceTrack.ValueType == typeof(int) &&
            sourceTrack.InterpolatorType ==
            typeof(StepAnimationInterpolator<int>))
        {
            target.AddTrack(
                id,
                BuildTrack<int>(
                    new StepAnimationInterpolator<int>(),
                    frames));
            return;
        }

        throw new NotSupportedException(
            $"Editing tracks with value type '{sourceTrack.ValueType.Name}' " +
            $"and interpolator '{sourceTrack.InterpolatorType.Name}' " +
            "is not supported yet.");
    }

    private static object ParseValue(
    Type valueType,
    string text)
    {
        text = text.Trim();

        if (valueType == typeof(float))
        {
            return ParseFiniteFloat(text);
        }

        if (valueType == typeof(int))
        {
            if (!int.TryParse(
                    text,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var integer))
            {
                throw new InvalidDataException(
                    "The keyframe value must be an integer.");
            }

            return integer;
        }

        if (valueType == typeof(Vector2))
        {
            var parts = ParseVectorParts(text, 2);

            return new Vector2(
                ParseFiniteFloat(parts[0]),
                ParseFiniteFloat(parts[1]));
        }

        if (valueType == typeof(Vector3))
        {
            var parts = ParseVectorParts(text, 3);

            return new Vector3(
                ParseFiniteFloat(parts[0]),
                ParseFiniteFloat(parts[1]),
                ParseFiniteFloat(parts[2]));
        }

        throw new NotSupportedException(
            $"Values of type '{valueType.Name}' cannot be edited yet.");
    }

    private static float ParseFiniteFloat(string text)
    {
        if (!float.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var value) ||
            !float.IsFinite(value))
        {
            throw new InvalidDataException(
                $"'{text}' must be a finite number.");
        }

        return value;
    }

    private static string[] ParseVectorParts(
        string text,
        int expectedCount)
    {
        text = text.Trim().Trim('(', ')');

        var parts = text.Split(
            ',',
            StringSplitOptions.TrimEntries);

        if (parts.Length != expectedCount)
        {
            throw new InvalidDataException(
                $"Expected {expectedCount} comma-separated vector components.");
        }

        return parts;
    }

    private static AnimationTrack<T> BuildTrack<T>(
        IAnimationInterpolator<T> interpolator,
        IReadOnlyList<(double TimeSeconds, object? Value)> frames)
    {
        var keyframes = frames
            .Select(frame =>
                new AnimationKeyframe<T>(
                    frame.TimeSeconds,
                    frame.Value is T typedValue
                        ? typedValue
                        : throw new InvalidDataException(
                            $"Invalid keyframe value for {typeof(T).Name}.")))
            .ToArray();

        return new AnimationTrackBuilder<T>(
            interpolator,
            keyframes).Build();
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
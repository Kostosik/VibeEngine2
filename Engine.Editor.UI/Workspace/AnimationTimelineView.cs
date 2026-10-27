using Engine.Animation;
using Engine.Core.Math;
using Engine.Editor.Documents;
using Engine.Graphics.Commands;
using Engine.Input;
using Engine.UI.Controls;
using Engine.UI.Core;
using Engine.UI.Input;
using Engine.UI.Layout;

namespace Engine.Editor.UI.Workspace;

public sealed class AnimationTimelineView : UiPanel
{
    private const float HeaderHeight = 30.0f;
    private const float TrackHeight = 36.0f;
    private const float LabelColumnWidth = 150.0f;
    private const float RightPadding = 12.0f;
    private const float MarkerSize = 10.0f;
    private const float MarkerHitRadius = 12.0f;

    private readonly AnimationClipDocument _document;

    private AnimationTrackId? _selectedTrackId;
    private double? _selectedKeyframeTime;

    private bool _dragging;
    private AnimationTrackId? _dragTrackId;
    private double _dragCurrentTime;

    public AnimationTimelineView(
        AnimationClipDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _document = document;

        Height = 170.0f;
        HorizontalAlignment = UiHorizontalAlignment.Stretch;
        VerticalAlignment = UiVerticalAlignment.Top;

        Background = new UiColor(20, 20, 20, 255);
    }

    public event Action<string>? MessageChanged;

    public void ReleaseResources()
    {
        _dragging = false;
        _dragTrackId = null;
    }

    protected override void OnRender(
        UiRenderContext context)
    {
        base.OnRender(context);

        var clip = _document.Clip;

        if (Bounds.Width <= LabelColumnWidth + RightPadding ||
            Bounds.Height <= HeaderHeight)
        {
            return;
        }

        context.PushClip(Bounds);

        try
        {
            DrawRuler(context, clip);

            var tracks = clip.Tracks
                .OrderBy(
                    static pair => pair.Key.Value,
                    StringComparer.Ordinal)
                .ToArray();

            for (var i = 0; i < tracks.Length; i++)
            {
                DrawTrack(
                    context,
                    tracks[i].Key,
                    tracks[i].Value,
                    i,
                    clip.DurationSeconds);
            }
        }
        finally
        {
            context.PopClip();
        }
    }

    private void DrawRuler(
        UiRenderContext context,
        AnimationClip clip)
    {
        var axisStart = AxisStartX;
        var axisWidth = AxisWidth;
        var axisEnd = axisStart + axisWidth;

        context.DrawText(
            "Track",
            new Vector2(Bounds.X + 8.0f, Bounds.Y + 8.0f),
            11.0f,
            new UiColor(180, 180, 180, 255));

        for (var i = 0; i <= 5; i++)
        {
            var fraction = i / 5.0f;
            var x = axisStart + axisWidth * (float)fraction;
            var time = clip.DurationSeconds * fraction;

            context.DrawRectangle(
                new UiRect(
                    x,
                    Bounds.Y + HeaderHeight - 5.0f,
                    1.0f,
                    MathF.Max(0.0f, Bounds.Height - HeaderHeight)),
                new UiColor(48, 48, 48, 255));

            context.DrawText(
                time.ToString("0.##"),
                new Vector2(x - 8.0f, Bounds.Y + 7.0f),
                10.0f,
                new UiColor(180, 180, 180, 255));
        }

        context.DrawRectangle(
            new UiRect(
                axisStart,
                Bounds.Y + HeaderHeight - 1.0f,
                axisEnd - axisStart,
                1.0f),
            new UiColor(90, 90, 90, 255));
    }

    private void DrawTrack(
        UiRenderContext context,
        AnimationTrackId id,
        IAnimationTrack track,
        int rowIndex,
        double durationSeconds)
    {
        var y = Bounds.Y + HeaderHeight + rowIndex * TrackHeight;

        var row = new UiRect(
            Bounds.X,
            y,
            Bounds.Width,
            TrackHeight);

        context.DrawRectangle(
            row,
            rowIndex % 2 == 0
                ? new UiColor(29, 29, 29, 255)
                : new UiColor(24, 24, 24, 255));

        context.DrawText(
            id.Value,
            new Vector2(Bounds.X + 8.0f, y + 10.0f),
            11.0f,
            new UiColor(210, 210, 210, 255));

        var axisStart = AxisStartX;
        var axisEnd = axisStart + AxisWidth;
        var centerY = y + TrackHeight / 2.0f;

        context.DrawRectangle(
            new UiRect(
                axisStart,
                centerY,
                axisEnd - axisStart,
                1.0f),
            new UiColor(70, 70, 70, 255));

        for (var i = 0; i < track.KeyframeCount; i++)
        {
            var time = track.GetKeyframeTimeSeconds(i);
            var x = TimeToX(time, durationSeconds);

            var selected =
                _selectedTrackId.HasValue &&
                _selectedTrackId.Value.Equals(id) &&
                _selectedKeyframeTime.HasValue &&
                Math.Abs(_selectedKeyframeTime.Value - time) < 0.0000001;

            context.DrawRectangle(
                new UiRect(
                    x - MarkerSize / 2.0f,
                    centerY - MarkerSize / 2.0f,
                    MarkerSize,
                    MarkerSize),
                selected
                    ? new UiColor(255, 190, 70, 255)
                    : new UiColor(90, 175, 245, 255));
        }
    }

    protected override void OnPointerDown(
        UiPointerEvent pointer)
    {
        if (pointer.Button != InputMouseButton.Left ||
            !Bounds.Contains(pointer.Position))
        {
            return;
        }

        var localY = pointer.Position.Y - Bounds.Y;

        if (localY < HeaderHeight)
        {
            return;
        }

        var rowIndex = (int)(
            (localY - HeaderHeight) / TrackHeight);

        var tracks = _document.Clip.Tracks
            .OrderBy(
                static pair => pair.Key.Value,
                StringComparer.Ordinal)
            .ToArray();

        if (rowIndex < 0 || rowIndex >= tracks.Length)
        {
            return;
        }

        var id = tracks[rowIndex].Key;
        var track = tracks[rowIndex].Value;
        var clip = _document.Clip;

        var localX = pointer.Position.X - Bounds.X;

        var bestDistance = float.MaxValue;
        var bestTime = 0.0;
        var found = false;

        for (var i = 0; i < track.KeyframeCount; i++)
        {
            var time = track.GetKeyframeTimeSeconds(i);
            var x = TimeToX(time, clip.DurationSeconds);
            var distance = MathF.Abs(localX - (x - Bounds.X));

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTime = time;
                found = true;
            }
        }

        if (!found || bestDistance > MarkerHitRadius)
        {
            _selectedTrackId = null;
            _selectedKeyframeTime = null;

            MessageChanged?.Invoke(
                "Click a keyframe marker to select or move it.");

            pointer.Handled = true;
            return;
        }

        _selectedTrackId = id;
        _selectedKeyframeTime = bestTime;

        _dragTrackId = id;
        _dragCurrentTime = bestTime;
        _dragging = true;

        pointer.RequestCapture();
        pointer.Handled = true;

        MessageChanged?.Invoke(
            $"Selected {id.Value} at {bestTime:0.###} s. Drag to move.");
    }

    protected override void OnPointerMove(
        UiPointerEvent pointer)
    {
        if (!_dragging ||
            pointer.Button != InputMouseButton.Left ||
            !_dragTrackId.HasValue)
        {
            return;
        }

        var clip = _document.Clip;

        var localX = pointer.Position.X - Bounds.X;
        var fraction = Math.Clamp(
            (localX - LabelColumnWidth) / AxisWidth,
            0.0f,
            1.0f);

        var targetTime = fraction * clip.DurationSeconds;

        // Snap to one thousand divisions of the clip duration.
        var step = clip.DurationSeconds / 1000.0;

        if (step > 0.0)
        {
            targetTime = (double)(
                Math.Round(targetTime / step) * step);
        }

        targetTime = Math.Clamp(
            targetTime,
            0.0,
            clip.DurationSeconds);

        if (Math.Abs(targetTime - _dragCurrentTime) < 0.0000001)
        {
            pointer.Handled = true;
            return;
        }

        try
        {
            MoveKeyframe(
                _dragTrackId.Value,
                _dragCurrentTime,
                targetTime);

            _dragCurrentTime = targetTime;
            _selectedKeyframeTime = targetTime;
        }
        catch (Exception exception)
        {
            MessageChanged?.Invoke(exception.Message);
        }

        pointer.Handled = true;
    }

    protected override void OnPointerUp(
        UiPointerEvent pointer)
    {
        if (!_dragging ||
            pointer.Button != InputMouseButton.Left)
        {
            return;
        }

        _dragging = false;
        _dragTrackId = null;

        pointer.ReleaseCapture();
        pointer.Handled = true;

        MessageChanged?.Invoke("Keyframe position updated.");
    }

    private void MoveKeyframe(
        AnimationTrackId id,
        double oldTime,
        double newTime)
    {
        var clip = _document.Clip;

        if (!clip.Tracks.TryGetValue(id, out var track))
        {
            throw new InvalidOperationException(
                $"Track '{id.Value}' no longer exists.");
        }

        var index = -1;

        for (var i = 0; i < track.KeyframeCount; i++)
        {
            if (Math.Abs(
                    track.GetKeyframeTimeSeconds(i) - oldTime)
                < 0.0000001)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            throw new InvalidOperationException(
                "The selected keyframe could not be found.");
        }

        var builder = new AnimationClipBuilder(clip);

        switch (track.ValueType)
        {
            case var type when type == typeof(float):
                builder.GetTrack<float>(id)
                    .MoveKeyframe(index, newTime);
                break;

            case var type when type == typeof(Vector2):
                builder.GetTrack<Vector2>(id)
                    .MoveKeyframe(index, newTime);
                break;

            case var type when type == typeof(Vector3):
                builder.GetTrack<Vector3>(id)
                    .MoveKeyframe(index, newTime);
                break;

            case var type when type == typeof(int):
                builder.GetTrack<int>(id)
                    .MoveKeyframe(index, newTime);
                break;

            default:
                throw new NotSupportedException(
                    $"Moving keyframes for '{track.ValueType.Name}' is not supported.");
        }

        _document.UpdateClip(builder.Build());
    }

    private float AxisStartX =>
        Bounds.X + LabelColumnWidth;

    private float AxisWidth =>
        MathF.Max(
            1.0f,
            Bounds.Width - LabelColumnWidth - RightPadding);

    private float TimeToX(
        double timeSeconds,
        double durationSeconds)
    {
        var fraction = Math.Clamp(
            timeSeconds / durationSeconds,
            0.0,
            1.0);

        return AxisStartX + AxisWidth * (float)fraction;
    }
}
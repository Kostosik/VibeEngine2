using System.Text.Json;
using Engine.Core.Math;

namespace Engine.Animation;

public sealed class AnimationClipJsonSerializer
{
    private const int CurrentFormatVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

    public string Serialize(AnimationClip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);

        var document = new ClipDocument
        {
            FormatVersion = CurrentFormatVersion,
            DurationSeconds = clip.DurationSeconds
        };

        foreach (var pair in clip.Tracks)
        {
            var track = pair.Value;
            var codec = GetCodec(track);

            var trackDocument = new TrackDocument
            {
                Id = pair.Key.Value,
                ValueType = codec.ValueType,
                Interpolator = codec.Interpolator
            };

            for (var i = 0; i < track.KeyframeCount; i++)
            {
                trackDocument.Keyframes.Add(
                    new KeyframeDocument
                    {
                        TimeSeconds =
                            track.GetKeyframeTimeSeconds(i),

                        Value = JsonSerializer.SerializeToElement(
                            track.GetKeyframeValue(i),
                            track.ValueType,
                            JsonOptions)
                    });
            }

            document.Tracks.Add(trackDocument);
        }

        return JsonSerializer.Serialize(
            document,
            JsonOptions);
    }

    public AnimationClip Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var document =
            JsonSerializer.Deserialize<ClipDocument>(
                json,
                JsonOptions)
            ?? throw new JsonException(
                "Animation document is empty.");

        if (document.FormatVersion != CurrentFormatVersion)
        {
            throw new NotSupportedException(
                $"Animation format version " +
                $"'{document.FormatVersion}' is not supported.");
        }

        if (document.Tracks is null)
        {
            throw new JsonException(
                "Animation tracks collection is null.");
        }

        var clip = new AnimationClip(
            document.DurationSeconds);

        foreach (var trackDocument in document.Tracks)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                trackDocument.Id);

            if (trackDocument.Keyframes is null)
            {
                throw new JsonException(
                    $"Track '{trackDocument.Id}' has no keyframe collection.");
            }

            var id = new AnimationTrackId(
                trackDocument.Id);

            switch (
                trackDocument.ValueType,
                trackDocument.Interpolator)
            {
                case ("float", "linear"):
                    clip.AddTrack(
                        id,
                        CreateTrack<float>(
                            trackDocument,
                            new FloatLinearAnimationInterpolator()));
                    break;

                case ("vector2", "linear"):
                    clip.AddTrack(
                        id,
                        CreateTrack<Vector2>(
                            trackDocument,
                            new Vector2LinearAnimationInterpolator()));
                    break;

                case ("vector3", "linear"):
                    clip.AddTrack(
                        id,
                        CreateTrack<Vector3>(
                            trackDocument,
                            new Vector3LinearAnimationInterpolator()));
                    break;

                case ("int", "step"):
                    clip.AddTrack(
                        id,
                        CreateTrack<int>(
                            trackDocument,
                            new StepAnimationInterpolator<int>()));
                    break;

                default:
                    throw new NotSupportedException(
                        $"Animation track type " +
                        $"'{trackDocument.ValueType}' with interpolator " +
                        $"'{trackDocument.Interpolator}' is not supported.");
            }
        }

        return clip;
    }

    private static AnimationTrack<T> CreateTrack<T>(
        TrackDocument document,
        IAnimationInterpolator<T> interpolator)
    {
        if (document.Keyframes.Count == 0)
        {
            throw new JsonException(
                $"Track '{document.Id}' contains no keyframes.");
        }

        var keyframes =
            new AnimationKeyframe<T>[document.Keyframes.Count];

        for (var i = 0; i < keyframes.Length; i++)
        {
            var source = document.Keyframes[i];

            var value = source.Value.Deserialize<T>(
                JsonOptions);

            keyframes[i] = new AnimationKeyframe<T>(
                source.TimeSeconds,
                value!);
        }

        return new AnimationTrack<T>(
            keyframes,
            interpolator);
    }

    private static TrackCodec GetCodec(
        IAnimationTrack track)
    {
        if (track.ValueType == typeof(float) &&
            track.InterpolatorType ==
                typeof(FloatLinearAnimationInterpolator))
        {
            return new TrackCodec("float", "linear");
        }

        if (track.ValueType == typeof(Vector2) &&
            track.InterpolatorType ==
                typeof(Vector2LinearAnimationInterpolator))
        {
            return new TrackCodec("vector2", "linear");
        }

        if (track.ValueType == typeof(Vector3) &&
            track.InterpolatorType ==
                typeof(Vector3LinearAnimationInterpolator))
        {
            return new TrackCodec("vector3", "linear");
        }

        if (track.ValueType == typeof(int) &&
            track.InterpolatorType ==
                typeof(StepAnimationInterpolator<int>))
        {
            return new TrackCodec("int", "step");
        }

        throw new NotSupportedException(
            $"Animation track type '{track.ValueType.FullName}' " +
            $"with interpolator '{track.InterpolatorType.FullName}' " +
            "cannot be serialized.");
    }

    private readonly record struct TrackCodec(
        string ValueType,
        string Interpolator);

    private sealed class ClipDocument
    {
        public ClipDocument()
        {
        }

        public int FormatVersion { get; set; }

        public double DurationSeconds { get; set; }

        public List<TrackDocument> Tracks { get; set; } = new();
    }

    private sealed class TrackDocument
    {
        public TrackDocument()
        {
        }

        public string Id { get; set; } = string.Empty;

        public string ValueType { get; set; } = string.Empty;

        public string Interpolator { get; set; } = string.Empty;

        public List<KeyframeDocument> Keyframes { get; set; } = new();
    }

    private sealed class KeyframeDocument
    {
        public KeyframeDocument()
        {
        }

        public double TimeSeconds { get; set; }

        public JsonElement Value { get; set; }
    }
}
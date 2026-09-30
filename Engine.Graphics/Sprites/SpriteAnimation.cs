using Engine.Animation;

namespace Engine.Graphics.Sprites;

public sealed class SpriteAnimation
{
    private static readonly AnimationTrackId FrameTrack = new("frame");

    private readonly Sprite[] _frames;
    private readonly AnimationClip _clip;
    private readonly AnimationPlayer _player;

    private int _currentFrame;

    public SpriteAnimation(
        IReadOnlyList<Sprite> frames,
        float frameDuration,
        bool loop = true)
    {
        ArgumentNullException.ThrowIfNull(frames);

        if (frames.Count == 0)
            throw new ArgumentException(
                "Animation must contain at least one frame.",
                nameof(frames));

        if (!float.IsFinite(frameDuration) || frameDuration <= 0.0f)
            throw new ArgumentOutOfRangeException(nameof(frameDuration));

        _frames = frames.ToArray();

        _clip = new AnimationClip(
            frames.Count * (double)frameDuration);

        var keyframes = new AnimationKeyframe<int>[frames.Count];

        for (var i = 0; i < frames.Count; i++)
        {
            keyframes[i] = new AnimationKeyframe<int>(
                i * (double)frameDuration,
                i);
        }

        _clip.AddTrack(
            FrameTrack,
            new AnimationTrack<int>(
                keyframes,
                new StepAnimationInterpolator<int>()));

        _player = new AnimationPlayer(_clip)
        {
            LoopMode = loop
                ? AnimationLoopMode.Loop
                : AnimationLoopMode.Once
        };

        _currentFrame = 0;

        _player.Play();
    }

    public Sprite Current => _frames[_currentFrame];

    public int CurrentFrame => _currentFrame;

    public int FrameCount => _frames.Length;

    public bool IsFinished => _player.IsFinished;

    public void Update(float deltaSeconds)
    {
        if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0.0f)
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

        _player.Update(deltaSeconds);

        _currentFrame = _clip.Sample<int>(
            FrameTrack,
            _player.TimeSeconds);
    }

    public void Reset()
    {
        _player.Reset();
        _currentFrame = 0;
        _player.Play();
    }
}
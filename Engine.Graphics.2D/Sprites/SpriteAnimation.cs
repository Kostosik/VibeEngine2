
using Engine.Animation;
using Engine.Core.Assets;
using Engine.Graphics.Resources;
using Engine.Core.Math;

namespace Engine.Graphics2D.Sprites;

public sealed class SpriteAnimation
{
    private static readonly AnimationTrackId FrameTrack = new("frame");

    private readonly Sprite[] _frames;
    private readonly AnimationClip _clip;
    private readonly AnimationPlayer _player;

    private int _currentFrame;
    private readonly double _frameDurationSeconds;
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
        _frameDurationSeconds = frameDuration;
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

    public SpriteAnimation(
        SpriteAnimationDefinition definition,
        ITextureResourceManager resources,
        float width = 1.0f,
        float height = 1.0f,
        int layer = 0)
        : this(
            CreateSprites(definition, resources, width, height, layer),
            definition.FrameDurationSeconds,
            definition.Loop)
    {
    }

    public Sprite Current => _frames[_currentFrame];

    public int CurrentFrame => _currentFrame;

    public int FrameCount => _frames.Length;

    public bool IsFinished => _player.IsFinished;

    public bool IsPlaying => _player.IsPlaying;

    public AnimationLoopMode LoopMode
    {
        get => _player.LoopMode;
        set => _player.LoopMode = value;
    }

    public double Speed
    {
        get => _player.Speed;
        set
        {
            if (!double.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value));

            _player.Speed = value;
        }
    }
    public event Action<int>? FrameChanged;
    public event Action? Completed
    {
        add => _player.Completed += value;
        remove => _player.Completed -= value;
    }

    public void Play()
    {
        _player.Play();
        RefreshCurrentFrame();
    }

    public void Pause()
    {
        _player.Pause();
    }

    public void Stop()
    {
        _player.Stop();
        RefreshCurrentFrame();
    }

    public void Reset()
    {
        _player.Reset();
        RefreshCurrentFrame();
        _player.Play();
    }

    public void Seek(double timeSeconds)
    {
        _player.Seek(timeSeconds);
        RefreshCurrentFrame();
    }

    public void Update(float deltaSeconds)
    {
        if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0.0f)
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

        var previousTime = _player.TimeSeconds;
        var movement = deltaSeconds * _player.Speed;
        var unwrappedNextTime = previousTime + movement;

        _player.Update(deltaSeconds);

        var nextTime = LoopMode == AnimationLoopMode.Loop
            ? unwrappedNextTime
            : _player.TimeSeconds;

        if (movement > 0.0)
            NotifyForwardTransitions(previousTime, nextTime);
        else if (movement < 0.0)
            NotifyBackwardTransitions(previousTime, nextTime);

        RefreshCurrentFrame();
    }

    private void NotifyForwardTransitions(
    double previousTime,
    double nextTime)
    {
        var firstStep = Math.Floor(previousTime / _frameDurationSeconds) + 1;
        var lastStep = Math.Floor(nextTime / _frameDurationSeconds);

        if (LoopMode == AnimationLoopMode.Once)
            lastStep = Math.Min(lastStep, FrameCount - 1);

        for (var step = firstStep; step <= lastStep; step++)
        {
            var frame = (int)(step % FrameCount);
            SetCurrentFrame(frame);
        }
    }

    private void NotifyBackwardTransitions(
    double previousTime,
    double nextTime)
    {
        // При движении назад граница в previousTime тоже учитывается.
        // Это важно, когда воспроизведение начинается точно на границе кадра.
        var firstStep = Math.Floor(
            previousTime / _frameDurationSeconds);

        var lastStep = Math.Floor(
            nextTime / _frameDurationSeconds) + 1;

        if (LoopMode == AnimationLoopMode.Once)
        {
            // В Once переход через конец клипа не меняет последний кадр.
            firstStep = Math.Min(firstStep, FrameCount - 1);
            lastStep = Math.Max(lastStep, 1);
        }

        for (var step = firstStep; step >= lastStep; step--)
        {
            var frame = (int)((step - 1) % FrameCount);

            if (frame < 0)
                frame += FrameCount;

            SetCurrentFrame(frame);
        }
    }

    private void SetCurrentFrame(int frame)
    {
        if (_currentFrame == frame)
            return;

        _currentFrame = frame;
        FrameChanged?.Invoke(frame);
    }

    private void RefreshCurrentFrame()
    {
        var frame = _clip.Sample<int>(
            FrameTrack,
            _player.TimeSeconds);

        SetCurrentFrame(frame);
    }

    private static IReadOnlyList<Sprite> CreateSprites(
        SpriteAnimationDefinition definition,
        ITextureResourceManager resources,
        float width,
        float height,
        int layer)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(resources);

        if (!float.IsFinite(width) ||
            !float.IsFinite(height) ||
            width <= 0.0f ||
            height <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                "Sprite dimensions must be finite and positive.");
        }

        if (definition.Frames.Count == 0)
        {
            throw new InvalidOperationException(
                "Sprite animation must contain at least one frame.");
        }

        var sprites = new Sprite[definition.Frames.Count];

        for (var i = 0; i < sprites.Length; i++)
        {
            var assetPath = new AssetPath(
                definition.Frames[i].TexturePath);

            var texture = resources.Load(assetPath);

            if (!resources.TryGetDescription(assetPath, out _))
            {
                throw new InvalidOperationException(
                    $"Could not read texture description for '{assetPath}'.");
            }

            sprites[i] = new Sprite(
                texture,
                new Rectangle(0.0f, 0.0f, 1.0f, 1.0f),
                new Vector2(width, height),
                layer);
        }

        return sprites;
    }
}
